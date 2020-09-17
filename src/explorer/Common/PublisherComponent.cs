namespace Explorer.Components
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using Explorer.Metrics;

    public interface PublisherComponent
    {
        public IAsyncEnumerable<ExploreMetric> YieldMetrics();

        public async Task PublishMetrics(MetricsPublisher publisher)
        {
            await foreach (var metric in YieldMetrics())
            {
                await publisher.PublishMetricAsync(metric);
            }
        }
    }
}