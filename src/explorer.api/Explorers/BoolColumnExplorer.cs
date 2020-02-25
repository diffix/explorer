namespace Explorer
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;

    using Aircloak.JsonApi.ResponseTypes;
    using Explorer.Queries;

    internal class BoolColumnExplorer : ExplorerImpl
    {
        public BoolColumnExplorer(IQueryResolver queryResolver, string tableName, string columnName)
            : base(queryResolver)
        {
            TableName = tableName;
            ColumnName = columnName;
        }

        public string TableName { get; set; }

        public string ColumnName { get; set; }

        public override async Task Explore()
        {
            var distinctValues = await ResolveQuery<DistinctColumnValues.BoolResult>(
                new DistinctColumnValues(TableName, ColumnName),
                timeout: TimeSpan.FromMinutes(2));

            var suppressedValueCount = distinctValues.ResultRows.Sum(row =>
                    row.ColumnValue.IsSuppressed ? row.Count : 0);

            PublishMetric(new ExploreResult.Metric(name: "suppressed_values", value: suppressedValueCount));

            var totalValueCount = distinctValues.ResultRows.Sum(row => row.Count);

            // This shouldn't happen, but check anyway.
            if (totalValueCount == 0)
            {
                throw new Exception(
                    $"Total value count for {TableName}, {ColumnName} is zero.");
            }

            PublishMetric(new ExploreResult.Metric(name: "total_count", value: totalValueCount));

            var suppressedValueRatio = (double)suppressedValueCount / totalValueCount;

            var distinctValueCounts =
                from row in distinctValues.ResultRows
                where !row.ColumnValue.IsSuppressed
                orderby row.Count descending
                select new
                {
                    Value = ((ValueColumn<bool>)row.ColumnValue).ColumnValue,
                    row.Count,
                };

            PublishMetric(new ExploreResult.Metric(name: "top_distinct_values", value: distinctValueCounts));
        }
    }
}
