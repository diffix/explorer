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

            decimal? estimate;
            decimal? result = null;

            estimate = await estimator(result, cancellationToken);

            for (var i = 0; i < MaxIterations; i++)
            {
                result = estimate;
                estimate = await estimator(result, cancellationToken);
                if ((!estimate.HasValue) ||
                    (isMin ? estimate >= result : estimate <= result) ||
                    (isMin && estimate == decimal.Zero))
                {
                    break;
                }
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