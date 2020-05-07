namespace Explorer.Explorers.Metrics
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    public interface MetricsPublisher
    {
        public bool PublishedMetrics(System.Guid id, out IEnumerable<ExploreMetric> metrics);

        public IEnumerable<ExploreMetric> PublishedMetrics(System.Guid id)
        {
            if (PublishedMetrics(id, out var metrics))
            {
                return metrics;
            }
            throw new KeyNotFoundException($"No Metrics available for id {id}.");
        }

        public void PublishMetric(System.Guid id, ExploreMetric metric);

        public void PublishMetrics(System.Guid id, MetricsProvider provider)
        {
            foreach (var metric in provider.Metrics())
            {
                PublishMetric(id, metric);
            }
        }

        public Task PublishMetricAsync(System.Guid id, ExploreMetric metric) =>
            Task.Run(() => PublishMetric(id, metric));

        public Task PublishMetricsAsync(System.Guid id, MetricsProvider provider) =>
            Task.WhenAll(provider.Metrics().Select(m => PublishMetricAsync(id, m)));
    }
}