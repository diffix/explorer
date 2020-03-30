namespace Explorer
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    using Explorer.Queries;

    internal class RealColumnExplorer : ExplorerBase
    {
        // TODO: The following should be configuration items (?)
        private const long ValuesPerBucketTarget = 20;

        private const double SuppressedRatioThreshold = 0.1;

        public RealColumnExplorer(IQueryResolver queryResolver, string tableName, string columnName)
            : base(queryResolver)
        {
            TableName = tableName;
            ColumnName = columnName;
        }

        private string TableName { get; }

        private string ColumnName { get; }

        public override async Task Explore(CancellationToken cancellationToken)
        {
            var statsQ = await ResolveQuery<NumericColumnStats.Result<double>>(
                new NumericColumnStats(TableName, ColumnName),
                cancellationToken);

            var stats = statsQ.ResultRows.Single();

            PublishMetric(new UntypedMetric(name: "naive_min", metric: stats.Min));
            PublishMetric(new UntypedMetric(name: "naive_max", metric: stats.Max));

            var distinctValueQ = await ResolveQuery<DistinctColumnValues.Result>(
                new DistinctColumnValues(TableName, ColumnName),
                cancellationToken);

            var suppressedValueCount = distinctValueQ.ResultRows.Sum(row =>
                row.DistinctData.IsSuppressed ? row.Count : 0);

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
                    where row.DistinctData.HasValue
                    orderby row.Count descending
                    select new
                    {
                        row.DistinctData.Value,
                        row.Count,
                    };

                PublishMetric(new UntypedMetric(name: "distinct.values", metric: distinctValues));
                PublishMetric(new UntypedMetric(
                    name: "distinct.null_count",
                    metric: distinctValueQ.ResultRows.Sum(row => row.DistinctData.IsNull ? row.Count : 0)));
                PublishMetric(new UntypedMetric(name: "distinct.suppressed_count", metric: suppressedValueCount));

                return;
            }

            var bucketsToSample = DiffixUtilities.EstimateBucketResolutions(
                stats.Count, stats.Min, stats.Max, ValuesPerBucketTarget);

            var histogramQ = await ResolveQuery<SingleColumnHistogram.Result>(
                new SingleColumnHistogram(TableName, ColumnName, bucketsToSample),
                cancellationToken);

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
                    && row.LowerBound.HasValue
                let lowerBound = row.LowerBound.Value
                let bucketSize = bucketsToSample[row.BucketIndex]
                orderby lowerBound
                select new
                {
                    BucketSize = bucketSize,
                    LowerBound = lowerBound,
                    row.Count,
                };

            PublishMetric(new UntypedMetric(name: "histogram.buckets", metric: histogramBuckets));
            PublishMetric(new UntypedMetric(name: "histogram.suppressed_count", metric: optimumBucket.SuppressedCount));
            PublishMetric(new UntypedMetric(name: "histogram.suppressed_ratio", metric: optimumBucket.Ratio));

            // Estimate Quartiles
            var processed = 0L;
            var quartileCount = totalValueCount / 4;
            var quartile = 1;
            var quartileEstimates = new List<double>();
            foreach (var bucket in histogramBuckets)
            {
                if (processed + bucket.Count < quartileCount * quartile)
                {
                    // no quartiles in this bucket
                    processed += bucket.Count;
                }
                else
                {
                    // one or more quartiles in this bucket
                    var remaining = bucket.Count;
                    var lowerBound = (double)bucket.LowerBound;
                    var range = (double)bucket.BucketSize;

                    do
                    {
                        var toProcess = (quartileCount * quartile) - processed;

                        if (toProcess > remaining)
                        {
                            processed += remaining;
                            break;
                        }

                        var subRange = (double)toProcess / remaining * range;
                        var quartileEstimate = lowerBound + subRange;

                        quartileEstimates.Add(quartileEstimate);

                        lowerBound = quartileEstimate;
                        range -= subRange;
                        processed += toProcess;
                        remaining -= toProcess;
                        quartile++;
                    }
                    while (remaining > 0 && quartile <= 3);

                    if (quartile > 3)
                    {
                        break;
                    }
                }
            }

            PublishMetric(new UntypedMetric(name: "quartile_estimates", metric: quartileEstimates));

            // Estimate Average
            var averageEstimate = histogramBuckets
                .Sum(bucket => bucket.Count * (bucket.LowerBound + (bucket.BucketSize / 2)))
                / totalValueCount;

            PublishMetric(new UntypedMetric(name: "avg_estimate", metric: decimal.Round(averageEstimate, 2)));
        }
    }
}
