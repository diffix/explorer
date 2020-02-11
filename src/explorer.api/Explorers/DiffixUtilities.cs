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
            dynamic minSample,
            dynamic maxSample,
            long valuesPerBucketTarget)
        {
            Debug.Assert(numSamples > 0, "Argument numSamples should always be greater than zero.");

            double valueDensity;
            try
            {
                valueDensity = (double)numSamples / (maxSample - minSample);
            }
            catch (System.DivideByZeroException)
            {
                return new List<decimal> { 1M };
            }

            var bucketSizeEstimate = new BucketSize(valuesPerBucketTarget / valueDensity);

            return (
                from bucketSize in new List<BucketSize>
                {
                    bucketSizeEstimate.Smaller(steps: 2),
                    bucketSizeEstimate,
                    bucketSizeEstimate.Larger(steps: 2),
                }
                select bucketSize.SnappedSize)
                .ToList();
        }
    }
}