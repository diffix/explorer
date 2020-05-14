namespace Explorer.Metrics
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    public interface MetricsPublisher
    {
        public IEnumerable<ExploreMetric> PublishedMetrics { get; }

        public void PublishMetric(ExploreMetric metric);

        public Task PublishMetricAsync(ExploreMetric metric) =>
            Task.Run(() => PublishMetric(metric));
    }
}