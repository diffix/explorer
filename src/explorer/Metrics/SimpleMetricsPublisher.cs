namespace Explorer.Metrics
{
    using System.Collections.Concurrent;
    using System.Collections.Generic;

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

        public IEnumerable<ExploreMetric> PublishedMetrics => store.Values;

        public void PublishMetric(ExploreMetric metric)
        {
            logger.LogDebug($"{nameof(SimpleMetricsPublisher)}: Publishing metric {metric.Name}.");
            store.AddOrUpdate(
                metric.Name,
                _ => metric,
                (_, old) => metric.Priority >= old.Priority ? metric : old);
        }
    }
}