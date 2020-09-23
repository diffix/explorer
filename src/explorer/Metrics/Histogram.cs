namespace Explorer.Metrics
{
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;

    using Explorer.Common.Utils;

    public class Histogram
    {
        private readonly decimal lowerBound;
        private readonly decimal upperBound;
        private readonly BucketSize bucketSize;

        public Histogram(IEnumerable<HistogramBucket> buckets, ValueCounts valueCounts)
        {
            ValueCounts = valueCounts;
            Buckets = buckets.OrderBy(b => b.LowerBound).ToList();
            bucketSize = Buckets[0].BucketSize;
            lowerBound = Buckets[0].LowerBound;
            upperBound = Buckets[^1].LowerBound + bucketSize.SnappedSize;

            Debug.Assert(
                buckets.All(b => b.BucketSize.SnappedSize == bucketSize.SnappedSize),
                "Histogram buckets don't match given bucketsize");
        }

        public IReadOnlyList<HistogramBucket> Buckets { get; }

        public ValueCounts ValueCounts { get; }

        public (decimal, decimal) GetBounds() => (upperBound, lowerBound);

        public decimal GetSnappedBucketSize() => bucketSize.SnappedSize;
    }
}