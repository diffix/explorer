namespace Explorer.Components
{
    using System.Collections.Generic;

    using Explorer.Metrics;

    public class QuartilesPublisher : PublisherComponent<QuartileEstimator.Result>
    {
        public QuartilesPublisher(ResultProvider<QuartileEstimator.Result> resultProvider)
        : base(resultProvider)
        {
        }

        public override IEnumerable<ExploreMetric> YieldMetrics(QuartileEstimator.Result result)
        {
            yield return new UntypedMetric(name: "quartile_estimates", metric: result.AsList);
        }
    }
}