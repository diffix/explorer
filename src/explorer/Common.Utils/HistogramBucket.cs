namespace Explorer.Common
{
    using System;

    public struct HistogramBucket : IEquatable<HistogramBucket>
    {
        private readonly NoisyCount noisyCount;

        internal HistogramBucket(decimal lowerBound, BucketSize bucketSize, NoisyCount noisyCount)
        {
            LowerBound = lowerBound;
            BucketSize = bucketSize;
            this.noisyCount = noisyCount;
        }

        public BucketSize BucketSize { get; }

        public decimal LowerBound { get; }

        public long Count => noisyCount.Count;

        public double CountNoise => noisyCount.Noise;

        public static bool operator ==(HistogramBucket left, HistogramBucket right) =>
            left.Equals(right);

        public static bool operator !=(HistogramBucket left, HistogramBucket right) =>
            !(left == right);

        public bool Equals(HistogramBucket bucket) =>
            noisyCount.Equals(bucket.noisyCount) &&
            BucketSize.Equals(bucket.BucketSize) &&
            LowerBound == bucket.LowerBound &&
            Count == bucket.Count &&
            CountNoise == bucket.CountNoise;

        public override bool Equals(object? obj) =>
            obj is HistogramBucket bucket && bucket.Equals(this);

        public override int GetHashCode() =>
            HashCode.Combine(noisyCount, BucketSize, LowerBound, Count, CountNoise);
    }
}
