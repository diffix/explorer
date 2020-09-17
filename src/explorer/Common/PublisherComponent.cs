namespace Explorer.Common
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

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