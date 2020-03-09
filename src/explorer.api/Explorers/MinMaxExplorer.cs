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
        public MinMaxExplorer(IQueryResolver queryResolver, string tableName, string columnName, CancellationToken ct)
            : base(queryResolver, ct)
        {
            TableName = tableName;
            ColumnName = columnName;
        }

        private delegate Task<decimal?> Estimator(decimal? bound = null);

        private string TableName { get; }

        private string ColumnName { get; }

        public override async Task Explore()
        {
            var minTask = RefinedEstimate(isMin: true);
            var maxTask = RefinedEstimate(isMin: false);

            await Task.WhenAll(minTask, maxTask);
        }

        private async Task RefinedEstimate(bool isMin)
        {
            var estimator = isMin ? (Estimator)GetMinEstimate : (Estimator)GetMaxEstimate;

            decimal? estimate;
            decimal? result = null;

            estimate = await estimator();

            while (estimate.HasValue && estimate != result)
            {
                result = estimate;
                estimate = await estimator(result);
            }

            Debug.Assert(result.HasValue, $"Unexpected null result when refining {(isMin ? "Min" : "Max")} estimate.");

            PublishMetric(new UntypedMetric(name: isMin ? "refined_min" : "refined_max", metric: result.Value));
        }

        private async Task<decimal?> GetMinEstimate(decimal? upperBound = null)
        {
            var minQ = await ResolveQuery<Min.Result<decimal>>(
                new Min(TableName, ColumnName, upperBound));
            return minQ.ResultRows.Single().Min;
        }

        private async Task<decimal?> GetMaxEstimate(decimal? lowerBound = null)
        {
            var maxQ = await ResolveQuery<Max.Result<decimal>>(
                new Max(TableName, ColumnName, lowerBound));
            return maxQ.ResultRows.Single().Max;
        }
    }
}