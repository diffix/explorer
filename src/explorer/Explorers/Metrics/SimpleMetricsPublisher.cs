namespace Explorer.Explorers.Metrics
{
    using System.Collections.Concurrent;
    using System.Collections.Generic;

    using Microsoft.Extensions.Logging;

    public class SimpleMetricsPublisher : MetricsPublisher
    {
        private readonly ConcurrentDictionary<System.Guid, ConcurrentDictionary<string, ExploreMetric>> store;
        private readonly ILogger<SimpleMetricsPublisher> logger;

        public SimpleMetricsPublisher(ILogger<SimpleMetricsPublisher> logger)
        {
            this.logger = logger;
            store = new ConcurrentDictionary<System.Guid, ConcurrentDictionary<string, ExploreMetric>>();
        }

        public bool PublishedMetrics(System.Guid id, out IEnumerable<ExploreMetric> metrics)
        {
            if (store.TryGetValue(id, out var metricsDict))
            {
                metrics = metricsDict.Values;
                return true;
            }
            metrics = System.Array.Empty<ExploreMetric>();
            return false;
        }

        public void PublishMetric(System.Guid id, ExploreMetric metric)
        {
            logger.LogDebug($"{nameof(SimpleMetricsPublisher)}: Publishing metric {metric.Name}.");
            store[id][metric.Name] = metric;
        }
    }
}