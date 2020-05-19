namespace Explorer.Components
{
    using System;
    using System.Collections.Generic;
    using Explorer.Metrics;

    public class InlinePublisher<T> : PublisherComponent<T>
    {
        private readonly Func<T, ExploreMetric> metricBuilder;

        public InlinePublisher(ResultProvider<T> resultProvider, Func<T, ExploreMetric> metricBuilder)
        : base(resultProvider)
        {
            this.metricBuilder = metricBuilder;
        }

        public override IEnumerable<ExploreMetric> YieldMetrics(T result)
        {
            yield return metricBuilder(result);
        }
    }
}