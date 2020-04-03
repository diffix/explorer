namespace Explorer.Explorers
{
    using System.Diagnostics;
    using System.Linq;
    using System.Threading.Tasks;

    using Diffix;
    using Explorer.Common;
    using Explorer.Queries;

    internal class MinMaxExplorer : ExplorerBase<ColumnExplorerContext>
    {
        private const int MaxIterations = 10;

        private delegate Task<decimal?> Estimator(DConnection conn, ColumnExplorerContext ctx, decimal? bound);

        public override async Task Explore(DConnection conn, ColumnExplorerContext ctx)
        {
            var minTask = RefinedEstimate(conn, ctx, isMin: true);
            var maxTask = RefinedEstimate(conn, ctx, isMin: false);

            await Task.WhenAll(minTask, maxTask);
        }

        private async Task RefinedEstimate(DConnection conn, ColumnExplorerContext ctx, bool isMin)
        {
            var estimator = isMin ? (Estimator)GetMinEstimate : (Estimator)GetMaxEstimate;

            // initial unconstrained min or max
            var result = await estimator(conn, ctx, null);

            // limit the number of iterations
            for (var i = 0; i < MaxIterations; i++)
            {
                // If it'a a minimum and we have a zero result, it can't be improved upon anyway.
                if (isMin && result == decimal.Zero)
                {
                    break;
                }

                // Constrained min/max query to get an improved estimate
                var estimate = await estimator(conn, ctx, result);

                // If there are no longer enough values in the constrained range to compute an anonymised min/max,
                // the query will return `null` => we can't improve further on the result.
                // Same thing if the results start to diverge (second part of if condition).
                if ((!estimate.HasValue) ||
                    (isMin ? estimate >= result : estimate <= result))
                {
                    break;
                }
                result = estimate;
            }

            Debug.Assert(result.HasValue, $"Unexpected null result when refining {(isMin ? "Min" : "Max")} estimate.");

            PublishMetric(new UntypedMetric(name: isMin ? "refined_min" : "refined_max", metric: result.Value));
        }

        private async Task<decimal?> GetMinEstimate(DConnection conn, ColumnExplorerContext ctx, decimal? upperBound)
        {
            var minQ = await conn.Exec<Min.Result<decimal>>(
                new Min(ctx.Table, ctx.Column, upperBound));
            return minQ.Rows.Single().Min;
        }

        private async Task<decimal?> GetMaxEstimate(DConnection conn, ColumnExplorerContext ctx, decimal? lowerBound)
        {
            var maxQ = await conn.Exec<Max.Result<decimal>>(
                new Max(ctx.Table, ctx.Column, lowerBound));
            return maxQ.Rows.Single().Max;
        }
    }
}