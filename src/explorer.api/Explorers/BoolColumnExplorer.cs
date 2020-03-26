namespace Explorer
{
    using System;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    using Explorer.Diffix.Extensions;
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

        public override async Task Explore(CancellationToken cancellationToken)
        {
            var distinctValuesQ = await ResolveQuery<DistinctColumnValues.Result>(
                new DistinctColumnValues(TableName, ColumnName),
                cancellationToken);

            var (totalValueCount, suppressedValueCount) = distinctValuesQ.ResultRows.CountTotalAndSuppressed();

            PublishMetric(new UntypedMetric(name: "distinct.suppressed_count", metric: suppressedValueCount));

            // This shouldn't happen, but check anyway.
            if (totalValueCount == 0)
            {
                throw new Exception(
                    $"Total value count for {TableName}, {ColumnName} is zero.");
            }

            PublishMetric(new UntypedMetric(name: "distinct.total_count", metric: totalValueCount));

            var suppressedValueRatio = (double)suppressedValueCount / totalValueCount;

            var distinctValueCounts =
                from row in distinctValuesQ.ResultRows
                where !row.DistinctData.IsSuppressed
                orderby row.Count descending
                select new
                {
                    row.DistinctData.Value,
                    row.Count,
                };

            PublishMetric(new UntypedMetric(name: "distinct.values", metric: distinctValueCounts));
        }
    }
}
