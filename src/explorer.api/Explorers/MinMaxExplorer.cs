namespace Explorer
{
    using System;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading.Tasks;

    using Explorer.Queries;

    internal class MinMaxExplorer : ExplorerImpl
    {
        public MinMaxExplorer(IQueryResolver queryResolver, string tableName, string columnName)
            : base(queryResolver)
        {
            TableName = tableName;
            ColumnName = columnName;
        }

        private delegate Task<decimal?> Estimator(decimal? bound = null);

        public string TableName { get; set; }

        public string ColumnName { get; set; }

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

            PublishMetric(new ExploreResult.Metric(name: isMin ? "refined_min" : "refined_max", value: result.Value));
        }

        private async Task<decimal?> GetMinEstimate(decimal? upperBound = null) =>
            (await ResolveQuery<Min.Result>(
                new Min(TableName, ColumnName, upperBound),
                timeout: TimeSpan.FromMinutes(2)))
                .ResultRows
                .Single()
                .Min;

        private async Task<decimal?> GetMaxEstimate(decimal? lowerBound = null) =>
            (await ResolveQuery<Max.Result>(
                new Max(TableName, ColumnName, lowerBound),
                timeout: TimeSpan.FromMinutes(2)))
                .ResultRows
                .Single()
                .Max;
    }
}