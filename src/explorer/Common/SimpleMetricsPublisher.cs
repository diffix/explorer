namespace Explorer.Common
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
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

        // TODO: replace MaybeNull with nullable type when C# 9 will be available
#pragma warning disable CS8653 // A default expression introduces a null value when 'T' is a non-nullable reference type.
        public bool TryFindMetric<T>(MetricDefinition<T> metricInfo, [MaybeNull] out T ret)
        where T : notnull
        {
            if (!store.TryGetValue(metricInfo.Name, out var metric))
            {
                ret = default;
                return false;
            }
            if (!(metric.Metric is T metricValue))
            {
                ret = default;
                return false;
            }
            ret = metricValue;
            return true;
        }
#pragma warning restore CS8653 // A default expression introduces a null value when 'T' is a non-nullable reference type.

        public T FindMetric<T>(MetricDefinition<T> metricInfo)
        where T : notnull
        {
            if (!store.TryGetValue(metricInfo.Name, out var metric))
            {
                throw new ArgumentException($"Value was not found for metric '{metricInfo.Name}`.");
            }
            if (!(metric.Metric is T metricValue))
            {
                throw new ArgumentException($"Incorrect type specified for metric '{metricInfo.Name}`.");
            }
            return metricValue;
        }
    }
}