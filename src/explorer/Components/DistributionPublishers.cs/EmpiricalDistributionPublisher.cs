namespace Explorer.Components
{
    using System.Collections.Generic;

    using Accord.Statistics.Distributions.Univariate;
    using Explorer.Metrics;

    public abstract class EmpiricalDistributionPublisher : PublisherComponent
    {
        private readonly ResultProvider<EmpiricalDistribution> distributionProvider;

        protected EmpiricalDistributionPublisher(ResultProvider<EmpiricalDistribution> distributionProvider)
        {
            this.distributionProvider = distributionProvider;
        }

        public async IAsyncEnumerable<ExploreMetric> YieldMetrics()
        {
            var dist = await distributionProvider.ResultAsync;

            foreach (var metric in EnumerateMetrics(dist))
            {
                yield return metric;
            }
        }

        protected abstract IEnumerable<ExploreMetric> EnumerateMetrics(EmpiricalDistribution distribution);
    }
}