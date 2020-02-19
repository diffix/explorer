namespace Explorer
{
    using System;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading.Tasks;

    using Aircloak.JsonApi;
    using Explorer.Api.Models;
    using Explorer.Queries;

    internal class MinMaxExplorer : ColumnExplorer
    {
        public MinMaxExplorer(JsonApiClient apiClient, ExploreParams exploreParams)
            : base(apiClient, exploreParams)
        {
        }

        private delegate Task<decimal?> Estimator(decimal? bound = null);

        public override async Task Explore()
        {
            LatestResult = new ExploreResult(ExplorationGuid, status: "waiting");

            var minTask = RefinedEstimate(isMin: true);
            var maxTask = RefinedEstimate(isMin: false);

            var results = await Task.WhenAll(minTask, maxTask);

            if (results.Any(r => r.MetricName == Status.Error))
            {
                var errors = results
                                .Where(r => r.MetricName == Status.Error)
                                .Select(r => r.MetricValue)
                                .ToList();

                LatestResult = new ExploreError(ExplorationGuid, string.Join("/n", errors));
                return;
            }

            LatestResult = new ExploreResult(ExplorationGuid, Status.Complete, results);
        }

        private async Task<ExploreResult.Metric> RefinedEstimate(bool isMin)
        {
            var estimator = isMin ? (Estimator)GetMinEstimate : (Estimator)GetMaxEstimate;

            decimal? estimate;
            decimal? result = null;

            try
            {
                estimate = await estimator();
            }
            catch (InvalidOperationException e)
                when (string.Equals(e.Message, "Sequence contains no elements", StringComparison.CurrentCulture))
            {
                // The initial estimate should always have a value (unless the dataset is empty?)
                // but check anyway. If we reach here, there is something wrong with the source data.
                var err =
                    $"Unable to obtain initial {(isMin ? "Min" : "Max")} estimate for " +
                    "{ExploreParams.TableName}, {ExploreParams.ColumnName}.";

                return new ExploreResult.Metric(name: Status.Error, value: err);
            }

            while (estimate.HasValue && estimate != result)
            {
                result = estimate;
                estimate = await estimator(result);
            }

            Debug.Assert(result.HasValue, $"Unexpected null result when refining {(isMin ? "Min" : "Max")} estimate.");

            return new ExploreResult.Metric(name: isMin ? "refined_min" : "refined_max", value: result.Value);
        }

        private async Task<decimal?> GetMinEstimate(decimal? upperBound = null) =>
            (await ResolveQuery<Min.Result>(
                new Min(ExploreParams.TableName, ExploreParams.ColumnName, upperBound),
                timeout: TimeSpan.FromMinutes(2)))
                .ResultRows
                .Single()
                .Min;

        private async Task<decimal?> GetMaxEstimate(decimal? lowerBound = null) =>
            (await ResolveQuery<Max.Result>(
                new Max(ExploreParams.TableName, ExploreParams.ColumnName, lowerBound),
                timeout: TimeSpan.FromMinutes(2)))
                .ResultRows
                .Single()
                .Max;
    }
}