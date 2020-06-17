namespace Explorer.Components
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;

    using Accord.Statistics.Distributions.Univariate;
    using Explorer.Common;

    public class EmpiricalDistributionComponent : ExplorerComponent<EmpiricalDistribution>
    {
        private readonly ResultProvider<NumericHistogramComponent.Result> histogramResultProvider;

        public EmpiricalDistributionComponent(ResultProvider<NumericHistogramComponent.Result> histogramResultProvider)
        {
            this.histogramResultProvider = histogramResultProvider;
        }

        public static EmpiricalDistribution GenerateDistribution(Histogram histogram)
        {
            var samples = histogram.Buckets.Values.Select(bucket =>
            {
                var sampleValue = bucket.LowerBound + (bucket.BucketSize.SnappedSize / 2);
                var sampleWeight = Convert.ToInt32(bucket.Count);

                return new
                {
                    SampleValue = sampleValue,
                    SampleWeight = sampleWeight,
                };
            });

            return new EmpiricalDistribution(
                samples.Select(_ => Convert.ToDouble(_.SampleValue)).ToArray(),
                samples.Select(_ => _.SampleWeight).ToArray());
        }

        protected override async Task<EmpiricalDistribution> Explore()
        {
            var histogramResult = await histogramResultProvider.ResultAsync;

            return GenerateDistribution(histogramResult.Histogram);
        }
    }
}