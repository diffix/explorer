namespace Explorer.Common
{
    public struct HistogramBucket
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
    }
}
