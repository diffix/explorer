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
        const double SuppressedRatioThreshold = 0.1;

        public IntegerColumnExplorer(JsonApiClient apiClient, ExploreParams exploreParams)
            : base(apiClient, exploreParams)
        {
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

            var suppressedValueCount = distinctValues.ResultRows.Count(row => row.ColumnValue.IsSuppressed);
            var totalValueCount = stats.ResultRows.First().Count;

            if (!totalValueCount.HasValue || totalValueCount.Value == 0)
            {
                yield return new ExploreError(ExplorationGuid,
                    $"Cannot explore table/column with value count {totalValueCount}.");
                yield break;
            }

            var suppressedValueRatio = (double)suppressedValueCount / totalValueCount.Value;

            if (suppressedValueRatio < SuppressedRatioThreshold)
            {
                // Only few of the values are suppressed. This means the data is already well-segmented and quite 
                // possibly categorical or quasi-categorical.
                var distinctMetrics = (from row in distinctValues.ResultRows
                                       where !row.ColumnValue.IsSuppressed
                                       let distinctValue = ((ValueColumn<long>)row.ColumnValue).ColumnValue
                                       let distinctCount = row.Count
                                       select new ExploreResult.Metric("distinctValue")
                                       {
                                           MetricValue = new { Value = distinctValue, Count = distinctCount }
                                       });
                yield return new ExploreResult(ExplorationGuid, status: "complete", metrics: distinctMetrics);
                yield break;
            }

            // var stats = rows.First();

            var obj = new { Hello = "hello", Num = 2 };

            yield return new ExploreResult(ExplorationGuid, status: "complete");
        }
    }
}
