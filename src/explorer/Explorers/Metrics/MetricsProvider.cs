namespace Explorer.Explorers.Metrics
{
    using System.Collections.Generic;

    public interface MetricsProvider
    {
        public IEnumerable<ExploreMetric> Metrics();
    }
}