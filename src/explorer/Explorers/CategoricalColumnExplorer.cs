namespace Explorer.Explorers
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;

    using Diffix;
    using Explorer.Common;
    using Explorer.Queries;

    internal class CategoricalColumnExplorer : ExplorerBase<ColumnExplorerContext>
    {
        public override async Task Explore(DConnection conn, ColumnExplorerContext ctx)
        {
            var distinctValues = await conn.Exec(
                new DistinctColumnValues(ctx.Table, ctx.Column));

            var counts = ValueCounts.Compute(distinctValues.Rows);

            PublishMetric(new UntypedMetric(name: "distinct.suppressed_count", metric: counts.SuppressedCount));

            // This shouldn't happen, but check anyway.
            if (counts.TotalCount == 0)
            {
                throw new Exception(
                    $"Total value count for {ctx.Table}, {ctx.Column} is zero.");
            }

            PublishMetric(new UntypedMetric(name: "distinct.total_count", metric: counts.TotalCount));

            var distinctValueCounts =
                from row in distinctValues.Rows
                where row.HasValue
                orderby row.Count descending
                select new
                {
                    row.Value,
                    row.Count,
                };

            PublishMetric(new UntypedMetric(name: "distinct.top_values", metric: distinctValueCounts.Take(10)));
        }
    }
}
