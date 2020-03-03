namespace Explorer
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;

    using Explorer.Queries;

    internal class BoolColumnExplorer : ExplorerBase
    {
        public BoolColumnExplorer(IQueryResolver queryResolver, string tableName, string columnName)
            : base(queryResolver)
        {
            TableName = tableName;
            ColumnName = columnName;
        }

        private string TableName { get; }

        private string ColumnName { get; }

        public override async Task Explore()
        {
            var distinctValues = await ResolveQuery<DistinctColumnValues.Result<bool>>(
                new DistinctColumnValues(TableName, ColumnName),
                timeout: TimeSpan.FromMinutes(2));

            var suppressedValueCount = distinctValues.ResultRows.Sum(row =>
                    row.DistinctData.IsSuppressed ? row.Count : 0);

            PublishMetric(new UntypedMetric(name: "suppressed_values", metric: suppressedValueCount));

            var totalValueCount = distinctValues.ResultRows.Sum(row => row.Count);

            // This shouldn't happen, but check anyway.
            if (totalValueCount == 0)
            {
                throw new Exception(
                    $"Total value count for {TableName}, {ColumnName} is zero.");
            }

            PublishMetric(new UntypedMetric(name: "total_count", metric: totalValueCount));

            var suppressedValueRatio = (double)suppressedValueCount / totalValueCount;

            var distinctValueCounts =
                from row in distinctValues.ResultRows
                where !row.DistinctData.IsSuppressed
                orderby row.Count descending
                select new
                {
                    row.DistinctData.Value,
                    row.Count,
                };

            PublishMetric(new UntypedMetric(name: "top_distinct_values", metric: distinctValueCounts));
        }
    }
}
