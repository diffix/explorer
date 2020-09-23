namespace Explorer.Components
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using Explorer.Common;
    using Explorer.Components.ResultTypes;
    using Explorer.Metrics;

    public class MinMaxFromHistogramComponent :
        ExplorerComponent<MinMaxFromHistogramComponent.Result>, PublisherComponent
    {
        private readonly ResultProvider<List<Histogram>> histogramsProvider;

        public MinMaxFromHistogramComponent(ResultProvider<List<Histogram>> histogramsProvider)
        {
            this.histogramsProvider = histogramsProvider;
        }

        public async IAsyncEnumerable<ExploreMetric> YieldMetrics()
        {
            var bounds = await ResultAsync;
            if (bounds == null)
            {
                yield break;
            }

            yield return ExploreMetric.Create(MetricDefinitions.Min, bounds.Min, priority: 10);
            yield return ExploreMetric.Create(MetricDefinitions.Max, bounds.Max, priority: 10);
        }

        protected override async Task<Result?> Explore()
        {
            var histograms = await histogramsProvider.ResultAsync;

            return histograms?
                .Where(r => r.ValueCounts.SuppressedCount == 0)
                .OrderBy(r => r.GetSnappedBucketSize())
                .Take(1)
                .Select(r => new Result(r.GetBounds()))
                .SingleOrDefault();
        }

        public class Result : NumericColumnBounds
        {
            internal Result((decimal, decimal) bounds)
            : base(bounds.Item1, bounds.Item2)
            {
            }
        }
    }
}