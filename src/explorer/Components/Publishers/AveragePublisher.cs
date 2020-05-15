namespace Explorer.Components
{
    using System.Collections.Generic;

    using Explorer.Metrics;

    public class AveragePublisher : PublisherComponent<AverageEstimator.Result>
    {
        public AveragePublisher(ResultProvider<AverageEstimator.Result> resultProvider)
        : base(resultProvider)
        {
        }

        public int Precision { get; set; }

        public override IEnumerable<ExploreMetric> YieldMetrics(AverageEstimator.Result result)
        {
            yield return new UntypedMetric(name: "average_estimate", metric: decimal.Round(result.Value, Precision));
        }
    }
}