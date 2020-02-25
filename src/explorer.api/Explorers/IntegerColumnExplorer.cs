namespace Explorer
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;

    using Aircloak.JsonApi.ResponseTypes;
    using Explorer.Queries;

    internal class IntegerColumnExplorer : ExplorerImpl
    {
        // TODO: The following should be configuration items (?)
        private const long ValuesPerBucketTarget = 20;

        private const double SuppressedRatioThreshold = 0.1;

        public IntegerColumnExplorer(IQueryResolver queryResolver, string tableName, string columnName)
            : base(queryResolver)
        {
            TableName = tableName;
            ColumnName = columnName;
        }

        public string TableName { get; set; }

        public string ColumnName { get; set; }

        public override async Task Explore()
        {
            var stats = (await ResolveQuery<NumericColumnStats.IntegerResult>(
                new NumericColumnStats(TableName, ColumnName),
                timeout: TimeSpan.FromMinutes(2)))
                .ResultRows
                .Single();

            PublishMetric(new UntypedMetric(name: "naive_min", metric: stats.Min));
            PublishMetric(new UntypedMetric(name: "naive_max", metric: stats.Max));

            var distinctValueQ = await ResolveQuery<DistinctColumnValues.IntegerResult>(
                new DistinctColumnValues(TableName, ColumnName),
                timeout: TimeSpan.FromMinutes(2));

            var suppressedValueCount = distinctValueQ.ResultRows.Sum(row =>
                    row.ColumnValue.IsSuppressed ? row.Count : 0);

            var totalValueCount = stats.Count;

            if (totalValueCount == 0)
            {
                throw new Exception(
                    $"Total value count for {TableName}, {ColumnName} is zero.");
            }

            var suppressedValueRatio = (double)suppressedValueCount / totalValueCount;

            if (suppressedValueRatio < SuppressedRatioThreshold)
            {
                // Only few of the values are suppressed. This means the data is already well-segmented and can be
                // considered categorical or quasi-categorical.
                var distinctValues =
                    from row in distinctValueQ.ResultRows
                    where !row.ColumnValue.IsSuppressed
                    select new
                    {
                        Value = ((ValueColumn<long>)row.ColumnValue).ColumnValue,
                        row.Count,
                    };

                PublishMetric(new UntypedMetric(name: "distinct_values", metric: distinctValues));
                PublishMetric(new UntypedMetric(name: "suppressed_values", metric: suppressedValueCount));

                return;
            }

            var bucketsToSample = DiffixUtilities.EstimateBucketResolutions(
                stats.Count, stats.Min, stats.Max, ValuesPerBucketTarget);

            var histogramQ = await ResolveQuery<SingleColumnHistogram.Result>(
                new SingleColumnHistogram(
                    TableName,
                    ColumnName,
                    bucketsToSample),
                timeout: TimeSpan.FromMinutes(10));

            var optimumBucket = (
                from row in histogramQ.ResultRows
                let suppressedRatio = (double)row.Count / totalValueCount
                let suppressedBucketSize = bucketsToSample[row.BucketIndex]
                where row.LowerBound.IsSuppressed
                    && suppressedRatio < SuppressedRatioThreshold
                orderby suppressedBucketSize
                select new
                {
                    Index = row.BucketIndex,
                    Size = suppressedBucketSize,
                    SuppressedCount = row.Count,
                    Ratio = suppressedRatio,
                }).First();

            var histogramBuckets =
                from row in histogramQ.ResultRows
                where row.BucketIndex == optimumBucket.Index
                    && !row.LowerBound.IsSuppressed
                let lowerBound = ((ValueColumn<decimal>)row.LowerBound).ColumnValue
                let bucketSize = bucketsToSample[row.BucketIndex]
                orderby lowerBound
                select new
                {
                    BucketSize = bucketSize,
                    LowerBound = lowerBound,
                    row.Count,
                };

            PublishMetric(new UntypedMetric(name: "histogram_buckets", metric: histogramBuckets));

            // Estimate Median
            var processed = 0;
            var target = (double)totalValueCount / 2;
            var medianEstimate = 0.0;
            foreach (var bucket in histogramBuckets)
            {
                if (processed + bucket.Count < target)
                {
                    processed += bucket.Count;
                }
                else
                {
                    var ratio = (target - processed) / bucket.Count;
                    medianEstimate =
                        (double)bucket.LowerBound + (ratio * (double)bucket.BucketSize);
                    break;
                }
            }

            PublishMetric(new UntypedMetric(name: "median_estimate", metric: (long)medianEstimate));

            // Estimate Average
            var averageEstimate = histogramBuckets
                .Sum(bucket => bucket.Count * (bucket.LowerBound + (bucket.BucketSize / 2)))
                / totalValueCount;

            PublishMetric(new UntypedMetric(name: "avg_estimate", metric: decimal.Round(averageEstimate, 2)));
        }
    }
}
