namespace Explorer
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;

    using Aircloak.JsonApi;
    using Explorer.Api.Models;
    using Explorer.Queries;

    internal class IntegerColumnExplorer : ColumnExplorer
    {
        const double SuppressedRatioThreshold = 0.1;

        public IEnumerable<ExploreResult.Metric> ExploreMetrics { get; set; }

        public IntegerColumnExplorer(JsonApiClient apiClient, ExploreParams exploreParams)
            : base(apiClient, exploreParams)
        {
            ExploreMetrics = Array.Empty<ExploreResult.Metric>();
        }

        public override async IAsyncEnumerable<ExploreResult> Explore()
        {
            yield return new ExploreResult(ExplorationGuid, status: "waiting");

            var stats = await ResolveQuery<NumericColumnStats.IntegerResult>(
                new NumericColumnStats(ExploreParams.TableName, ExploreParams.ColumnName),
                timeout: TimeSpan.FromMinutes(2));

            var distinctValues = await ResolveQuery<DistinctColumnValues.IntegerResult>(
                new DistinctColumnValues(ExploreParams.TableName, ExploreParams.ColumnName),
                timeout: TimeSpan.FromMinutes(2));

            Debug.Assert(
                stats.ResultRows.Count() == 1,
                $"Expected query NumericColumnStats query to return exactly one row.");

            var suppressedValueCount = distinctValues.ResultRows.Sum(row =>
                    row.ColumnValue.IsSuppressed ? row.Count : 0);
            var totalValueCount = stats.ResultRows.First().Count;

            if (!totalValueCount.HasValue || totalValueCount.Value == 0)
            {
                yield return new ExploreError(
                    ExplorationGuid,
                    $"Cannot explore table/column with value count {totalValueCount}.");
                yield break;
            }

            // Note: suppressedValueCount should never be null for this query.
            var suppressedValueRatio = (double)suppressedValueCount / totalValueCount.Value;

            if (suppressedValueRatio < SuppressedRatioThreshold)
            {
                // Only few of the values are suppressed. This means the data is already well-segmented and can be
                // considered categorical or quasi-categorical.
                var distinctMetrics =
                    from row in distinctValues.ResultRows
                    select new
                    {
                        Value = row.ColumnValue,
                        row.Count,
                    };

                ExploreMetrics = ExploreMetrics.Append(
                    new ExploreResult.Metric(name: "distinct_values", value: distinctMetrics));

                yield return new ExploreResult(
                    ExplorationGuid,
                    status: "complete",
                    metrics: ExploreMetrics);

                yield break;
            }

            // determine approximate bucket size from min/max bounds
            var columnStats = stats.ResultRows.First();
            var valueDensity = (double)(columnStats.Count ?? 0) / (columnStats.Max - columnStats.Min)
                ?? throw new Exception($"Unable to calculate value density from column stats {columnStats}");


            Debug.Assert(valueDensity > 0, "Column Count should always be greater than zero.");

            const double ValuesPerBucketTarget = 20; // TODO: should be a configuration item (?)
            var bucketSizeEstimate = new BucketSize(ValuesPerBucketTarget / valueDensity);

            var bucketsToSample = (
                from bucketSize in new List<BucketSize> {
                    bucketSizeEstimate.Smaller(steps: 2),
                    bucketSizeEstimate,
                    bucketSizeEstimate.Larger(steps: 2) }
                select bucketSize.SnappedSize)
                .ToList();

            var histogramQ = await ResolveQuery<SingleColumnHistogram.Result>(
                new SingleColumnHistogram(
                    ExploreParams.TableName,
                    ExploreParams.ColumnName,
                    bucketsToSample),
                timeout: TimeSpan.FromMinutes(10));

            var suppressedCountByBucketSize =
                from row in histogramQ.ResultRows
                where row.LowerBound.IsSuppressed
                select new
                {
                    // Note: row.BucketIndex should never be null for this query type.
                    BucketSize = bucketsToSample[row.BucketIndex.Value],
                    row.Count,
                };

            var histogramMetrics =
                from row in histogramQ.ResultRows
                where row.BucketIndex.HasValue
                select new
                {
                    // Note: row.BucketIndex should never be null for this query type.
                    BucketSize = bucketsToSample[row.BucketIndex.Value],
                    row.LowerBound,
                    row.Count,
                };

            ExploreMetrics = ExploreMetrics
                    .Append(new ExploreResult.Metric(name: "histogram_buckets", value: histogramMetrics))
                    .Append(new ExploreResult.Metric(name: "suppressed_count", value: suppressedCountByBucketSize));

            yield return new ExploreResult(ExplorationGuid, status: "complete", metrics: ExploreMetrics);
        }
    }
}
