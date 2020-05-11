namespace Explorer.Explorers.Metrics
{
    using System.Collections.Concurrent;

    using Microsoft.Extensions.Logging;

    public class SimpleMetricsPublisher : MetricsPublisher
    {
        private readonly ConcurrentDictionary<string, ExploreMetric> store;
        private readonly ILogger<SimpleMetricsPublisher> logger;

        public SimpleMetricsPublisher(ILogger<SimpleMetricsPublisher> logger)
        {
            this.logger = logger;
            store = new ConcurrentDictionary<string, ExploreMetric>();
        }

        public void PublishMetric(ExploreMetric metric)
        {
            logger.LogDebug($"{nameof(SimpleMetricsPublisher)}: Publishing metric {metric.Name}.");
            store[metric.Name] = metric;
        }
    }
}