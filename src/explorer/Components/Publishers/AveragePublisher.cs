namespace Explorer.Components
{
    using System.Collections.Generic;

    using Explorer.Metrics;

    public class AveragePublisher : PublisherComponent
    {
        private readonly ResultProvider<AverageEstimator.Result> resultProvider;

        public AveragePublisher(
            ResultProvider<AverageEstimator.Result> resultProvider)
        {
            this.resultProvider = resultProvider;
        }

        public int Precision { get; set; }

        public override async IAsyncEnumerable<ExploreMetric> YieldMetrics()
        {
            var result = await resultProvider.ResultAsync;

            yield return new UntypedMetric(name: "average_estimate", metric: decimal.Round(result.Value, Precision));
        }
    }
}