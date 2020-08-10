namespace Explorer.Components
{
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading.Tasks;

    using Diffix;
    using Explorer.Common;
    using Explorer.Components.ResultTypes;
    using Explorer.Metrics;
    using Explorer.Queries;

    public class MinMaxRefiner : ExplorerComponent<MinMaxRefiner.Result>, PublisherComponent
    {
        private const int MaxIterations = 10;
        private readonly ResultProvider<SimpleStats<decimal>.Result> statsProvider;
        private readonly ResultProvider<MinMaxFromHistogramComponent.Result> histogramMinMaxProvider;

        public MinMaxRefiner(
            ResultProvider<SimpleStats<decimal>.Result> statsProvider,
            ResultProvider<MinMaxFromHistogramComponent.Result> histogramMinMaxProvider)
        {
            this.statsProvider = statsProvider;
            this.histogramMinMaxProvider = histogramMinMaxProvider;
        }

        public async IAsyncEnumerable<ExploreMetric> YieldMetrics()
        {
            var bounds = await ResultAsync;
            if (bounds == null)
            {
                yield break;
            }

            yield return new UntypedMetric("min", bounds.Min, priority: 10);
            yield return new UntypedMetric("max", bounds.Max, priority: 10);
        }

        protected override async Task<Result?> Explore()
        {
            var stats = await statsProvider.ResultAsync;
            if (stats == null)
            {
                return null;
            }
            if (stats.Min == null || stats.Max == null)
            {
                return null;
            }

            var histogramBounds = await histogramMinMaxProvider.ResultAsync;
            return histogramBounds is null
                ? new Result(await RefinedMinEstimate(), await RefinedMaxEstimate())
                : null;
        }

        private async Task<decimal> RefinedMinEstimate()
        {
            // initial unconstrained min or max
            var result = await GetMinEstimate(null);
            var isPositive = result >= 0;

            // limit the number of iterations
            for (var i = 0; i < MaxIterations; i++)
            {
                // If we have a zero result, it can't be improved upon anyway.
                if (isPositive && result == decimal.Zero)
                {
                    break;
                }

                // Constrained query to get an improved estimate
                var estimate = await GetMinEstimate(result);

                // If there are no longer enough values in the constrained range to compute an anonymised min/max,
                // the query will return `null` => we can't improve further on the result.
                // Same thing if the results start to diverge, ie. if the current result no longer improves on the
                // previous result (second part of if condition).
                if ((!estimate.HasValue) || (estimate >= result))
                {
                    break;
                }
                result = estimate;
            }

            Debug.Assert(result.HasValue, "Unexpected null result when refining Min estimate.");

            return result.Value;
        }

        private async Task<decimal> RefinedMaxEstimate()
        {
            // initial unconstrained min or max
            var result = await GetMaxEstimate(null);
            var isNegative = result < 0;

            // limit the number of iterations
            for (var i = 0; i < MaxIterations; i++)
            {
                // If we are working with negative numbers and have a zero result, assume we can't improve it.
                if (isNegative && result == decimal.Zero)
                {
                    break;
                }

                // Constrained query to get an improved estimate
                var estimate = await GetMaxEstimate(result);

                // If there are no longer enough values in the constrained range to compute an anonymised min/max,
                // the query will return `null` => we can't improve further on the result.
                // Same thing if the results start to diverge (second part of if condition).
                if ((!estimate.HasValue) || (estimate <= result))
                {
                    break;
                }
                result = estimate;
            }

            Debug.Assert(result.HasValue, "Unexpected null result when refining Max estimate.");

            return result.Value;
        }

        private async Task<decimal?> GetMinEstimate(decimal? upperBound)
        {
            var minQ = await Context.Exec<Min, Min.Result<decimal>>(new Min(upperBound));
            return minQ.Rows.Single().Min;
        }

        private async Task<decimal?> GetMaxEstimate(decimal? lowerBound)
        {
            var maxQ = await Context.Exec<Max, Max.Result<decimal>>(new Max(lowerBound));
            return maxQ.Rows.Single().Max;
        }

        public class Result : NumericColumnBounds
        {
            internal Result(decimal min, decimal max)
            : base(min, max)
            {
            }
        }
    }
}