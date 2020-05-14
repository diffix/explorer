namespace Explorer.Components
{
    using System.Collections.Generic;

    using Explorer.Metrics;

    public class MinMaxPublisher : PublisherComponent
    {
        private readonly ResultProvider<MinMaxRefiner.Result> resultProvider;

        public MinMaxPublisher(
            MetricsPublisher publisher,
            ResultProvider<MinMaxRefiner.Result> resultProvider)
        : base(publisher)
        {
            this.resultProvider = resultProvider;
        }

        public int Precision { get; set; }

        public override async IAsyncEnumerable<ExploreMetric> YieldMetrics()
        {
            var result = await resultProvider.ResultAsync;

            yield return new UntypedMetric("refined_max", result.Max);
            yield return new UntypedMetric("refined_min", result.Min);
        }
    }
}