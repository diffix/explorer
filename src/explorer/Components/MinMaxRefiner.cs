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
        private readonly ExplorerContext ctx;
        private readonly ResultProvider<MinMaxFromHistogramComponent.Result> histogramMinMaxProvider;

        public MinMaxRefiner(
            ExplorerContext ctx,
            ResultProvider<MinMaxFromHistogramComponent.Result> histogramMinMaxProvider)
        {
            this.ctx = ctx;
            this.histogramMinMaxProvider = histogramMinMaxProvider;
        }

        public async IAsyncEnumerable<ExploreMetric> YieldMetrics()
        {
            var bounds = await ResultAsync;

            if (!(bounds is null))
            {
                yield return new UntypedMetric("min", bounds.Min, priority: 10);
                yield return new UntypedMetric("max", bounds.Max, priority: 10);
            }
        }

        protected override async Task<Result> Explore()
        {
            var histogramBounds = await histogramMinMaxProvider.ResultAsync;

#pragma warning disable CS8603 // Possible null reference return
            return histogramBounds is null
                ? new Result(await RefinedMinEstimate(), await RefinedMaxEstimate())
                : null;
#pragma warning restore CS8603 // Possible null reference return
        }

        private async Task<decimal> RefinedMinEstimate()
        {
            // initial unconstrained min or max
            var result = await GetMinEstimate(null);

            // limit the number of iterations
            for (var i = 0; i < MaxIterations; i++)
            {
                // If we have a zero result, it can't be improved upon anyway.
                if (result == decimal.Zero)
                {
                    break;
                }

                // Constrained query to get an improved estimate
                var estimate = await GetMinEstimate(result);

                // If there are no longer enough values in the constrained range to compute an anonymised min/max,
                // the query will return `null` => we can't improve further on the result.
                // Same thing if the results start to diverge (second part of if condition).
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

            // limit the number of iterations
            for (var i = 0; i < MaxIterations; i++)
            {
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
            var minQ = await ctx.Exec<Min.Result<decimal>>(
                new Min(ctx.Table, ctx.Column, upperBound));
            return minQ.Rows.Single().Min;
        }

        private async Task<decimal?> GetMaxEstimate(decimal? lowerBound)
        {
            var maxQ = await ctx.Exec<Max.Result<decimal>>(
                new Max(ctx.Table, ctx.Column, lowerBound));
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