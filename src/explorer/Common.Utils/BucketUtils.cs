namespace Explorer.Common.Utils
{
    using System.Linq;

    internal static class BucketUtils
    {
        internal static decimal[] EstimateBucketResolutions(
            long numSamples,
            double minSample,
            double maxSample,
            long valuesPerBucketTarget,
            bool isIntegerColumn)
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

            var targetBucketSize = valuesPerBucketTarget / valueDensity;

            var bucketSizeEstimate = new BucketSize(isIntegerColumn
                ? System.Math.Max(targetBucketSize, 5)
                : targetBucketSize);

            return new[]
            {
                bucketSizeEstimate.Smaller(steps: 2),
                bucketSizeEstimate,
                bucketSizeEstimate.Larger(steps: 2),
            }
            .Where(b => !(b is null))
            .Select(b => b!.SnappedSize)
            .Distinct()
            .ToArray();
        }
    }
}
