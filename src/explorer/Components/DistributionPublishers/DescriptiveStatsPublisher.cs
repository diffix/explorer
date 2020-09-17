namespace Explorer.Components
{
    using System.Collections.Generic;

    using Explorer.Common;

    public class DescriptiveStatsPublisher : PublisherComponent
    {
        private readonly ResultProvider<NumericDistribution> distributionProvider;

        public DescriptiveStatsPublisher(ResultProvider<NumericDistribution> distributionProvider)
        {
            this.distributionProvider = distributionProvider;
        }

        public async IAsyncEnumerable<ExploreMetric> YieldMetrics()
        {
            var distribution = await distributionProvider.ResultAsync;
            if (distribution == null)
            {
                yield break;
            }

            yield return new UntypedMetric(
                name: "descriptive_stats",
                metric: distribution);
        }
    }
}