namespace Explorer.Components
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using Explorer.Components.ResultTypes;
    using Explorer.Metrics;

    public class MinMaxFromHistogramComponent :
        ExplorerComponent<MinMaxFromHistogramComponent.Result>, PublisherComponent
    {
        private readonly ResultProvider<List<HistogramWithCounts>> histogramsProvider;

        public MinMaxFromHistogramComponent(ResultProvider<List<HistogramWithCounts>> histogramsProvider)
        {
            this.histogramsProvider = histogramsProvider;
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
            var histograms = await histogramsProvider.ResultAsync;

            return histograms
                .Where(r => r.ValueCounts.SuppressedCount == 0)
                .OrderBy(r => r.BucketSize.SnappedSize)
                .Take(1)
                .Select(r => new Result(r.Histogram.Bounds))
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