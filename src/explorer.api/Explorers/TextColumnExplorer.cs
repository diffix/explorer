namespace Explorer
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using Aircloak.JsonApi;
    using Aircloak.JsonApi.ResponseTypes;
    using Explorer.Api.Models;
    using Explorer.Queries;

    internal class TextColumnExplorer : ExplorerImpl
    {
        public TextColumnExplorer(JsonApiClient apiClient, ExploreParams exploreParams)
            : base(apiClient, exploreParams)
        {
            ExploreMetrics = Array.Empty<ExploreResult.Metric>();
        }

        public IEnumerable<ExploreResult.Metric> ExploreMetrics { get; set; }

        public override async Task Explore()
        {
            var distinctValues = await ResolveQuery<DistinctColumnValues.TextResult>(
                new DistinctColumnValues(ExploreParams.TableName, ExploreParams.ColumnName),
                timeout: TimeSpan.FromMinutes(2));

            var suppressedValueCount = distinctValues.ResultRows.Sum(row =>
                    row.ColumnValue.IsSuppressed ? row.Count : 0);

            var totalValueCount = distinctValues.ResultRows.Sum(row => row.Count);

            // This shouldn't happen, but check anyway.
            if (totalValueCount == 0)
            {
                throw new Exception(
                    $"Total value count for {ExploreParams.TableName}, {ExploreParams.ColumnName} is zero.");
            }

            var suppressedValueRatio = (double)suppressedValueCount / totalValueCount;

            var distinctValueCounts =
                from row in distinctValues.ResultRows
                where !row.ColumnValue.IsSuppressed
                orderby row.Count descending
                select new
                {
                    Value = ((ValueColumn<string>)row.ColumnValue).ColumnValue,
                    row.Count,
                };

            PublishMetric(new ExploreResult.Metric(name: "top_distinct_values", value: distinctValueCounts.Take(10)));
            PublishMetric(new ExploreResult.Metric(name: "total_count", value: totalValueCount));
            PublishMetric(new ExploreResult.Metric(name: "suppressed_values", value: suppressedValueCount));
        }
    }
}
