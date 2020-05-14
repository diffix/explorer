namespace Explorer.Components
{
    using System.Collections.Generic;

    using Explorer.Metrics;

    public class QuartilesPublisher : PublisherComponent
    {
        private readonly ResultProvider<QuartileEstimator.Result> resultProvider;

        public QuartilesPublisher(
            MetricsPublisher publisher,
            ResultProvider<QuartileEstimator.Result> resultProvider)
        : base(publisher)
        {
            this.resultProvider = resultProvider;
        }

        public override async IAsyncEnumerable<ExploreMetric> YieldMetrics()
        {
            var result = await resultProvider.ResultAsync;

            yield return new UntypedMetric(name: "quartile_estimates", metric: result.AsList);
        }
    }
}