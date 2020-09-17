namespace Explorer.Common
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    public interface MetricsPublisher
    {
        public IEnumerable<ExploreMetric> PublishedMetrics { get; }

        public void PublishMetric(ExploreMetric metric);

        public async Task PublishMetricAsync(ExploreMetric metric)
        {
            await Task.CompletedTask;
            PublishMetric(metric);
        }
    }
}