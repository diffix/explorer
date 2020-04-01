namespace Explorer
{
    using System;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    using Explorer.Diffix.Extensions;
    using Explorer.Queries;

    internal class CategoricalColumnExplorer : ExplorerBase
    {
        public CategoricalColumnExplorer(IQueryResolver queryResolver, string tableName, string columnName)
            : base(queryResolver)
        {
            TableName = tableName;
            ColumnName = columnName;
        }

        private string TableName { get; }

        private string ColumnName { get; }

        public override async Task Explore(CancellationToken cancellationToken)
        {
            var distinctValuesQ = await ResolveQuery<DistinctColumnValues.Result>(
                new DistinctColumnValues(TableName, ColumnName),
                cancellationToken);

            var counts = ValueCounts.Compute(distinctValuesQ.ResultRows);

            PublishMetric(new UntypedMetric(name: "distinct.suppressed_count", metric: counts.SuppressedCount));

            // This shouldn't happen, but check anyway.
            if (counts.TotalCount == 0)
            {
                throw new Exception(
                    $"Total value count for {TableName}, {ColumnName} is zero.");
            }

            PublishMetric(new UntypedMetric(name: "distinct.total_count", metric: counts.TotalCount));

            var distinctValueCounts =
                from row in distinctValuesQ.ResultRows
                where row.DistinctData.HasValue
                orderby row.Count descending
                select new
                {
                    row.DistinctData.Value,
                    row.Count,
                };

            PublishMetric(new UntypedMetric(name: "distinct.top_values", metric: distinctValueCounts.Take(10)));
        }
    }
}
