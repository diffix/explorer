namespace Explorer
{
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;

    using Explorer.Queries;

    internal static class DiffixUtilities
    {
        internal static List<decimal> EstimateBucketResolutions(
            long numSamples,
            double minSample,
            double maxSample,
            long valuesPerBucketTarget)
        {
            Debug.Assert(numSamples > 0, "Argument numSamples should always be greater than zero.");

            var range = maxSample - minSample;

            Debug.Assert(range > 0, "Data range must be greater than zero.");

            var valueDensity = (double)numSamples / (maxSample - minSample);

            return EstimateBucketResolutions(valuesPerBucketTarget, valueDensity);
        }

        internal static List<decimal> EstimateBucketResolutions(
            long numSamples,
            long minSample,
            long maxSample,
            long valuesPerBucketTarget)
        {
            Debug.Assert(numSamples > 0, "Argument numSamples should always be greater than zero.");

            var range = maxSample - minSample;

            Debug.Assert(range > 0, "Data range must be greater than zero.");

            var valueDensity = (double)numSamples / (maxSample - minSample);

            return EstimateBucketResolutions(valuesPerBucketTarget, valueDensity);
        }

        private static List<decimal> EstimateBucketResolutions(
            long valuesPerBucketTarget,
            double valueDensityEstimate)
        {
            var bucketSizeEstimate = new BucketSize(valuesPerBucketTarget / valueDensityEstimate);

            var bucketList = new List<BucketSize>
            {
                bucketSizeEstimate.Smaller(steps: 2),
                bucketSizeEstimate,
                bucketSizeEstimate.Larger(steps: 2),
            };

            return bucketList.Select(x => x.SnappedSize).ToList();
        }
    }
}