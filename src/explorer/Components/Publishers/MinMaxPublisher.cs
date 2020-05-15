namespace Explorer.Components
{
    using System.Collections.Generic;

    using Explorer.Metrics;

    public class MinMaxPublisher : PublisherComponent
    {
        private readonly ResultProvider<MinMaxRefiner.Result> resultProvider;

        public MinMaxPublisher(
            ResultProvider<MinMaxRefiner.Result> resultProvider)
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