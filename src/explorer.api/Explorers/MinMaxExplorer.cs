namespace Explorer
{
    using System;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    using Explorer.Queries;

    internal class MinMaxExplorer : ExplorerBase
    {
        private const int MaxIterations = 10;

        public MinMaxExplorer(IQueryResolver queryResolver, string tableName, string columnName)
            : base(queryResolver)
        {
            TableName = tableName;
            ColumnName = columnName;
        }

        private delegate Task<decimal?> Estimator(decimal? bound, CancellationToken cancellationToken);

        private string TableName { get; }

        private string ColumnName { get; }

        public override async Task Explore(CancellationToken cancellationToken)
        {
            var minTask = RefinedEstimate(isMin: true, cancellationToken);
            var maxTask = RefinedEstimate(isMin: false, cancellationToken);

            await Task.WhenAll(minTask, maxTask);
        }

        private async Task RefinedEstimate(bool isMin, CancellationToken cancellationToken)
        {
            var estimator = isMin ? (Estimator)GetMinEstimate : (Estimator)GetMaxEstimate;

            // initial unconstrained min or max
            var result = await estimator(null, cancellationToken);

            // limit the number of iterations
            for (var i = 0; i < MaxIterations; i++)
            {
                // If it'a a minimum and we have a zero result, it can't be improved upon anyway.
                if (isMin && result == decimal.Zero)
                {
                    break;
                }

                // Constrained min/max query to get an improved estimate
                var estimate = await estimator(result, cancellationToken);

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

        private async Task<decimal?> GetMinEstimate(decimal? upperBound, CancellationToken cancellationToken)
        {
            var minQ = await ResolveQuery<Min.Result<decimal>>(
                new Min(TableName, ColumnName, upperBound),
                cancellationToken);
            return minQ.ResultRows.Single().Min;
        }

        private async Task<decimal?> GetMaxEstimate(decimal? lowerBound, CancellationToken cancellationToken)
        {
            var maxQ = await ResolveQuery<Max.Result<decimal>>(
                new Max(TableName, ColumnName, lowerBound),
                cancellationToken);
            return maxQ.ResultRows.Single().Max;
        }
    }
}