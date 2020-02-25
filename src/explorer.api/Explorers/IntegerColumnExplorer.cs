namespace Explorer
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;

    using Aircloak.JsonApi;
    using Aircloak.JsonApi.ResponseTypes;
    using Explorer.Api.Models;
    using Explorer.Queries;

    internal class IntegerColumnExplorer : ExplorerImpl
    {
        // TODO: The following should be configuration items (?)
        private const long ValuesPerBucketTarget = 20;

        private const double SuppressedRatioThreshold = 0.1;

        public IntegerColumnExplorer(JsonApiClient apiClient, ExploreParams exploreParams)
            : base(apiClient, exploreParams)
        {
        }

        public override async Task Explore()
        {
            var stats = (await ResolveQuery<NumericColumnStats.IntegerResult>(
                new NumericColumnStats(ExploreParams.TableName, ExploreParams.ColumnName),
                timeout: TimeSpan.FromMinutes(2)))
                .ResultRows
                .Single();

            PublishMetric(new ExploreResult.Metric(name: "naive_min", value: stats.Min));
            PublishMetric(new ExploreResult.Metric(name: "naive_max", value: stats.Max));

            var distinctValueQ = await ResolveQuery<DistinctColumnValues.IntegerResult>(
                new DistinctColumnValues(ExploreParams.TableName, ExploreParams.ColumnName),
                timeout: TimeSpan.FromMinutes(2));

            var suppressedValueCount = distinctValueQ.ResultRows.Sum(row =>
                    row.ColumnValue.IsSuppressed ? row.Count : 0);

            var totalValueCount = stats.Count;

            if (totalValueCount == 0)
            {
                throw new Exception(
                    $"Total value count for {ExploreParams.TableName}, {ExploreParams.ColumnName} is zero.");
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

                PublishMetric(new ExploreResult.Metric(name: "distinct_values", value: distinctValues));
                PublishMetric(new ExploreResult.Metric(name: "suppressed_values", value: suppressedValueCount));

                return;
            }

            var bucketsToSample = DiffixUtilities.EstimateBucketResolutions(
                stats.Count, stats.Min, stats.Max, ValuesPerBucketTarget);

            var histogramQ = await ResolveQuery<SingleColumnHistogram.Result>(
                new SingleColumnHistogram(
                    ExploreParams.TableName,
                    ExploreParams.ColumnName,
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

            PublishMetric(new ExploreResult.Metric(name: "histogram_buckets", value: histogramBuckets));

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

            PublishMetric(new ExploreResult.Metric(name: "median_estimate", value: (long)medianEstimate));

            // Estimate Average
            var averageEstimate = histogramBuckets
                .Sum(bucket => bucket.Count * (bucket.LowerBound + (bucket.BucketSize / 2)))
                / totalValueCount;

            PublishMetric(new ExploreResult.Metric(name: "avg_estimate", value: decimal.Round(averageEstimate, 2)));
        }
    }
}
