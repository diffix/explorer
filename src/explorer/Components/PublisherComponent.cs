namespace Explorer.Components
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using Explorer.Metrics;

    public abstract class PublisherComponent
    {
        private readonly MetricsPublisher publisher;

        protected PublisherComponent(MetricsPublisher publisher)
        {
            this.publisher = publisher;
        }

        public abstract IAsyncEnumerable<ExploreMetric> YieldMetrics();

        public virtual async Task PublishMetricsAsync()
        {
            await foreach (var metric in YieldMetrics())
            {
                await publisher.PublishMetricAsync(metric);
            }
        }
    }
}