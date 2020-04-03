namespace Explorer.Common
{
    using System.Diagnostics;

    internal static class BucketUtils
    {
        internal static decimal[] EstimateBucketResolutions(
            long numSamples,
            double minSample,
            double maxSample,
            long valuesPerBucketTarget)
        {
            if (numSamples <= 0)
            {
                throw new System.ArgumentException(
                    $"Argument numSamples should always be greater than zero, got {numSamples}.");
            }

            var range = maxSample - minSample;

            if (range <= 0)
            {
                return new decimal[] { 1M };
            }

            var valueDensity = numSamples / (maxSample - minSample);

            return EstimateBucketResolutions(valuesPerBucketTarget, valueDensity);
        }

        internal static decimal[] EstimateBucketResolutions(
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

        private static decimal[] EstimateBucketResolutions(
            long valuesPerBucketTarget,
            double valueDensityEstimate)
        {
            var bucketSizeEstimate = new BucketSize(valuesPerBucketTarget / valueDensityEstimate);

            return new decimal[]
            {
                bucketSizeEstimate.Smaller(steps: 2).SnappedSize,
                bucketSizeEstimate.SnappedSize,
                bucketSizeEstimate.Larger(steps: 2).SnappedSize,
            };
        }
    }
}
