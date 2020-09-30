namespace Explorer.Components
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using Explorer.Common;
    using Explorer.Metrics;

    public class HistogramSelectorComponent :
        ExplorerComponent<Histogram>, PublisherComponent
    {
        private readonly ResultProvider<DistinctValuesComponent.Result> distinctValuesProvider;
        private readonly ResultProvider<List<Histogram>> histogramsProvider;

        public HistogramSelectorComponent(
            ResultProvider<DistinctValuesComponent.Result> distinctValuesProvider,
            ResultProvider<List<Histogram>> histogramsProvider)
        {
            this.distinctValuesProvider = distinctValuesProvider;
            this.histogramsProvider = histogramsProvider;
        }

        public async IAsyncEnumerable<ExploreMetric> YieldMetrics()
        {
            var result = await ResultAsync;
            if (result == null)
            {
                yield break;
            }
            yield return ExploreMetric.Create(MetricDefinitions.Histogram, result);
        }

        protected override async Task<Histogram?> Explore()
        {
            var distinctValues = await distinctValuesProvider.ResultAsync;
            if (distinctValues == null)
            {
                return null;
            }
            if (distinctValues.IsCategorical)
            {
                return null;
            }

            var histograms = await histogramsProvider.ResultAsync;

            return histograms?
                .OrderBy(h => h.SnappedBucketSize)
                .ThenBy(h => h.ValueCounts.SuppressedCount)
                .First();
        }
    }
}