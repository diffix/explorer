namespace Explorer
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;

    using Aircloak.JsonApi;
    using Aircloak.JsonApi.ResponseTypes;
    using Explorer.Api.Models;
    using Explorer.Queries;

    internal class IntegerColumnExplorer : ColumnExplorer
    {
        // TODO: The following should be configuration items (?)
        private const double ValuesPerBucketTarget = 20;

        private const double SuppressedRatioThreshold = 0.1;

        public IEnumerable<ExploreResult.Metric> ExploreMetrics { get; set; }

        public IntegerColumnExplorer(JsonApiClient apiClient, ExploreParams exploreParams)
            : base(apiClient, exploreParams)
        {
            ExploreMetrics = Array.Empty<ExploreResult.Metric>();
        }

        public IEnumerable<ExploreResult.Metric> ExploreMetrics { get; set; }

        public override async IAsyncEnumerable<ExploreResult> Explore()
        {
            yield return new ExploreResult(ExplorationGuid, status: "waiting");

            var stats = (await ResolveQuery<NumericColumnStats.IntegerResult>(
                new NumericColumnStats(ExploreParams.TableName, ExploreParams.ColumnName),
                timeout: TimeSpan.FromMinutes(2)))
                .ResultRows
                .Single();

            var distinctValueQ = await ResolveQuery<DistinctColumnValues.IntegerResult>(
                new DistinctColumnValues(ExploreParams.TableName, ExploreParams.ColumnName),
                timeout: TimeSpan.FromMinutes(2));

            var totalValueCount = stats.Count ?? 0;

            if (totalValueCount == 0)
            {
                yield return new ExploreError(
                    ExplorationGuid,
                    $"Cannot explore table/column: Invalid value count.");
                yield break;
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

                ExploreMetrics = ExploreMetrics
                    .Append(new ExploreResult.Metric(name: "distinct_values", value: distinctValues))
                    .Append(new ExploreResult.Metric(name: "suppressed_values", value: suppressedValueCount));

                yield return new ExploreResult(
                    ExplorationGuid,
                    status: "complete",
                    metrics: ExploreMetrics);

                yield break;
            }

            // determine approximate bucket size from min/max bounds
            var valueDensity = (double)(stats.Count ?? 0) / (stats.Max - stats.Min)
                ?? throw new Exception($"Unable to calculate value density from column stats {stats}");

            Debug.Assert(valueDensity > 0, "Column Count should always be greater than zero.");

            var bucketSizeEstimate = new BucketSize(ValuesPerBucketTarget / valueDensity);

            var bucketsToSample = (
                from bucketSize in new List<BucketSize>
                {
                    bucketSizeEstimate.Smaller(steps: 2),
                    bucketSizeEstimate,
                    bucketSizeEstimate.Larger(steps: 2),
                }
                select bucketSize.SnappedSize)
                .ToList();

            var histogramQ = await ResolveQuery<SingleColumnHistogram.Result>(
                new SingleColumnHistogram(
                    ExploreParams.TableName,
                    ExploreParams.ColumnName,
                    bucketsToSample),
                timeout: TimeSpan.FromMinutes(10));

            // Note: row.BucketIndex and row.Count should never be null in this context.
            var optimumBucket = (
                from row in histogramQ.ResultRows
                let suppressedRatio = (double)row.Count.Value / totalValueCount
                let suppressedBucketSize = bucketsToSample[row.BucketIndex.Value]
                where row.LowerBound.IsSuppressed
                    && suppressedRatio < SuppressedRatioThreshold
                orderby suppressedBucketSize
                select new
                {
                    Index = row.BucketIndex.Value,
                    Size = suppressedBucketSize,
                    SuppressedCount = row.Count ?? 0,
                    Ratio = suppressedRatio,
                }).First();

            // Note: row.BucketIndex should never be null for this query type.
            var histogramBuckets =
                from row in histogramQ.ResultRows
                where row.BucketIndex.HasValue
                    && row.BucketIndex == optimumBucket.Index
                    && !row.LowerBound.IsSuppressed
                let lowerBound = ((ValueColumn<decimal>)row.LowerBound).ColumnValue
                let bucketSize = bucketsToSample[row.BucketIndex.Value]
                orderby lowerBound
                select new
                {
                    BucketSize = bucketSize,
                    LowerBound = lowerBound,
                    Count = row.Count ?? 0,
                };

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

            // Estimate Average
            var averageEstimate = histogramBuckets
                .Sum(bucket => bucket.Count * (bucket.LowerBound + (bucket.BucketSize / 2)))
                / totalValueCount;

            ExploreMetrics = ExploreMetrics
                    .Append(new ExploreResult.Metric(name: "histogram_buckets", value: histogramBuckets))
                    .Append(new ExploreResult.Metric(name: "median_estimate", value: (long)medianEstimate))
                    .Append(new ExploreResult.Metric(name: "avg_estimate", value: decimal.Round(averageEstimate, 2)))
                    .Append(new ExploreResult.Metric(name: "min_estimate", value: (long)stats.Min))
                    .Append(new ExploreResult.Metric(name: "max_estimate", value: (long)stats.Max));

            yield return new ExploreResult(ExplorationGuid, status: "complete", metrics: ExploreMetrics);
        }
    }
}
