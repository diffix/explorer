namespace Explorer.Components
{
    using System.Collections.Generic;

    using Accord.Statistics.Distributions.Univariate;
    using Explorer.Metrics;

    public class DescriptiveStatsPublisher : EmpiricalDistributionPublisher
    {
        public DescriptiveStatsPublisher(ResultProvider<EmpiricalDistribution> distributionProvider)
        : base(distributionProvider)
        {
        }

        protected override IEnumerable<ExploreMetric> EnumerateMetrics(EmpiricalDistribution distribution)
        {
            var blob = new
            {
                distribution.Entropy,
                distribution.Mean,
                distribution.Mode,
                Quartiles = new double[]
                {
                    distribution.Quartiles.Min,
                    distribution.Median,
                    distribution.Quartiles.Max,
                },
                distribution.StandardDeviation,
                distribution.Variance,
            };

            yield return new UntypedMetric(
                name: "descriptive_stats",
                metric: blob);
        }
    }
}