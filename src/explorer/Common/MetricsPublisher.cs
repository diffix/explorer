namespace Explorer.Common
{
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Threading.Tasks;

    public interface MetricsPublisher
    {
        public IEnumerable<ExploreMetric> PublishedMetrics { get; }

        public void PublishMetric(ExploreMetric metric);

        public async Task PublishMetricAsync(ExploreMetric metric)
        {
            await Task.CompletedTask;
            PublishMetric(metric);
        }

        public T FindMetric<T>(MetricDefinition<T> metricInfo)
        where T : notnull;

        public bool TryFindMetric<T>(MetricDefinition<T> metricInfo, [MaybeNull] out T ret)
        where T : notnull;
    }
}