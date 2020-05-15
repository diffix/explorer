namespace Explorer.Components
{
    using System.Collections.Generic;

    using Explorer.Metrics;

    public class SimpleStatsPublisher<T> : PublisherComponent
    {
        private readonly ResultProvider<SimpleStats<T>.Result> resultProvider;

        public SimpleStatsPublisher(
            MetricsPublisher publisher,
            ResultProvider<SimpleStats<T>.Result> resultProvider)
        : base(publisher)
        {
            this.resultProvider = resultProvider;
        }

        public override async IAsyncEnumerable<ExploreMetric> YieldMetrics()
        {
            var stats = await resultProvider.ResultAsync;

            yield return new UntypedMetric("count", stats.Count);
            yield return new UntypedMetric("naive_min", stats.Min!);
            yield return new UntypedMetric("naive_max", stats.Max!);
        }
    }
}