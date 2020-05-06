namespace Explorer.Explorers.Metrics
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    internal interface MetricsPublisher
    {
        public IEnumerable<ExploreMetric> PublishedMetrics { get; }

        public void PublishMetric(ExploreMetric metric);

        public void PublishMetrics(MetricsProvider provider)
        {
            foreach (var metric in provider.Metrics())
            {
                PublishMetric(metric);
            }
        }

        public Task PublishMetricAsync(ExploreMetric metric) => Task.Run(() => PublishMetric(metric));

        public Task PublishMetricsAsync(MetricsProvider provider) =>
            Task.WhenAll(provider.Metrics().Select(PublishMetricAsync));
    }
}