namespace Explorer.Components
{
    using System.Collections.Generic;

    using Explorer.Metrics;

    public class SimpleStatsPublisher<T> : PublisherComponent<SimpleStats<T>.Result>
    {
        public SimpleStatsPublisher(ResultProvider<SimpleStats<T>.Result> resultProvider)
        : base(resultProvider)
        {
        }

        public override IEnumerable<ExploreMetric> YieldMetrics(SimpleStats<T>.Result stats)
        {
            yield return new UntypedMetric("count", stats.Count);
            yield return new UntypedMetric("naive_min", stats.Min!);
            yield return new UntypedMetric("naive_max", stats.Max!);
        }
    }
}