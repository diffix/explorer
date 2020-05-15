namespace Explorer.Components
{
    using System.Collections.Generic;

    using Explorer.Metrics;

    public class MinMaxPublisher : PublisherComponent<MinMaxRefiner.Result>
    {
        public MinMaxPublisher(ResultProvider<MinMaxRefiner.Result> resultProvider)
        : base(resultProvider)
        {
        }

        public int Precision { get; set; }

        public override IEnumerable<ExploreMetric> YieldMetrics(MinMaxRefiner.Result result)
        {
            yield return new UntypedMetric("refined_max", result.Max);
            yield return new UntypedMetric("refined_min", result.Min);
        }
    }
}