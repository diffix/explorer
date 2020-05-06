
namespace Explorer.Explorers.Metrics
{
    using System.Collections.Concurrent;
    using System.Collections.Generic;

    internal class SimpleMetricsPublisher : MetricsPublisher
    {
        private readonly ConcurrentDictionary<string, ExploreMetric> store;

        public SimpleMetricsPublisher()
        {
            store = new ConcurrentDictionary<string, ExploreMetric>();
        }

        public IEnumerable<ExploreMetric> PublishedMetrics
        {
            get => store.Values;
        }

        public void PublishMetric(ExploreMetric metric)
        {
            store[metric.Name] = metric;
        }
    }
}