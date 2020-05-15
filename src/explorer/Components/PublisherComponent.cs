namespace Explorer.Components
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using Explorer.Metrics;

    public abstract class PublisherComponent
    {
        public abstract IAsyncEnumerable<ExploreMetric> YieldMetrics();

        public virtual async Task PublishMetricsAsync(MetricsPublisher publisher)
        {
            await foreach (var metric in YieldMetrics())
            {
                await publisher.PublishMetricAsync(metric);
            }
        }
    }
}