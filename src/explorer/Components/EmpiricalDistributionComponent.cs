namespace Explorer.Components
{
    using System.Collections.Generic;
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
            var interpolatedHistogramSamples = histogram.Buckets.Values.SelectMany(bucket =>
            {
                var bucketSize = (double)bucket.BucketSize.SnappedSize;
                var sampleSpacing = bucketSize / bucket.Count;
                var sample = (double)bucket.LowerBound + (0.5 * sampleSpacing);
                var upperBound = (double)bucket.LowerBound + bucketSize;
                var samples = new List<double>((int)bucket.Count);

                while (sample < upperBound)
                {
                    samples.Add(sample);
                    sample += sampleSpacing;
                }

                return samples;
            });

            return new EmpiricalDistribution(interpolatedHistogramSamples.ToArray());
        }

        protected override async Task<EmpiricalDistribution> Explore()
        {
            var histogramResult = await histogramResultProvider.ResultAsync;

            return await Task.Run(() => GenerateDistribution(histogramResult.Histogram));
        }
    }
}