namespace Explorer.Components.ResultTypes
{
    using Explorer.Common.Utils;

    public class HistogramWithCounts
    {
        internal HistogramWithCounts(ValueCounts valueCounts, Histogram histogram)
        {
            ValueCounts = valueCounts;
            Histogram = histogram;
        }

        public decimal SnappedBucketSize => Histogram.GetSnappedBucketSize();

        public ValueCounts ValueCounts { get; }

        public Histogram Histogram { get; }
    }
}