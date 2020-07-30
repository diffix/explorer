namespace Explorer.Components.ResultTypes
{
    using Explorer.Common;

    public class HistogramWithCounts
    {
        internal HistogramWithCounts(ValueCounts valueCounts, Histogram histogram)
        {
            ValueCounts = valueCounts;
            Histogram = histogram;
        }

        public BucketSize BucketSize => Histogram.BucketSize;

        public ValueCounts ValueCounts { get; }

        public Histogram Histogram { get; }
    }
}