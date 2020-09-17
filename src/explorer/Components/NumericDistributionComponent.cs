namespace Explorer.Components
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;

    using Accord.Statistics.Distributions.Univariate;
    using Explorer.Common;
    using Explorer.Components.ResultTypes;

    public class NumericDistributionComponent : ExplorerComponent<NumericDistribution>
    {
        private readonly ResultProvider<HistogramWithCounts> histogramResultProvider;

        public NumericDistributionComponent(ResultProvider<HistogramWithCounts> histogramResultProvider)
        {
            this.histogramResultProvider = histogramResultProvider;
        }

        public static NumericDistribution? GenerateDistribution(HistogramWithCounts histogramResult)
        {
            if (histogramResult.ValueCounts.NonSuppressedNonNullCount == 0)
            {
                return null;
            }

            var samples = histogramResult.Histogram.Buckets.Values.Select(bucket =>
            {
                var sampleValue = bucket.LowerBound + (bucket.BucketSize.SnappedSize / 2);
                var sampleWeight = Convert.ToInt32(bucket.Count);

                return new
                {
                    SampleValue = sampleValue,
                    SampleWeight = sampleWeight,
                };
            });

            var dist = new EmpiricalDistribution(
                samples.Select(_ => Convert.ToDouble(_.SampleValue)).ToArray(),
                samples.Select(_ => _.SampleWeight).ToArray());

            return new NumericDistribution(dist);
        }

        protected override async Task<NumericDistribution?> Explore()
        {
            var histogramResult = await histogramResultProvider.ResultAsync;
            if (histogramResult == null)
            {
                return null;
            }

            return GenerateDistribution(histogramResult);
        }
    }
}