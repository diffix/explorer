namespace Explorer.Components
{
    using System.Collections.Generic;

    using Explorer.Metrics;

    public class SimpleStatsPublisher<T> : PublisherComponent
    {
        private readonly ResultProvider<SimpleStats<T>.Result> resultProvider;

        public SimpleStatsPublisher(
            ResultProvider<SimpleStats<T>.Result> resultProvider)
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