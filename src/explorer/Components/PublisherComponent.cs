namespace Explorer.Components
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using Explorer.Metrics;

    public interface PublisherComponent
    {
        public Task PublishMetricsAsync(MetricsPublisher publisher);
    }

    public abstract class PublisherComponent<TResult> : PublisherComponent
    {
        private readonly ResultProvider<TResult> resultProvider;

        protected PublisherComponent(ResultProvider<TResult> resultProvider)
        {
            this.resultProvider = resultProvider;
        }

        public abstract IEnumerable<ExploreMetric> YieldMetrics(TResult result);

        public virtual async Task PublishMetricsAsync(MetricsPublisher publisher)
        {
            foreach (var metric in YieldMetrics(await resultProvider.ResultAsync))
            {
                await publisher.PublishMetricAsync(metric);
            }
        }
    }
}