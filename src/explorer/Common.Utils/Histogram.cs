namespace Explorer.Common.Utils
{
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;

    using Explorer.Queries;

    public class Histogram
    {
        public Histogram(BucketSize bucketSize, IEnumerable<HistogramBucket> buckets)
        {
            Debug.Assert(
                buckets.All(b => b.BucketSize.SnappedSize == bucketSize.SnappedSize),
                "Histogram buckets don't match given bucketsize");

            BucketSize = bucketSize;
            Buckets = new SortedList<decimal, HistogramBucket>(buckets.ToDictionary(b => b.LowerBound));
        }

        public BucketSize BucketSize { get; }

        public SortedList<decimal, HistogramBucket> Buckets { get; }

        public (decimal, decimal) Bounds => (
                    Buckets.First().Value.LowerBound,
                    Buckets.Last().Value.LowerBound + BucketSize.SnappedSize);

        public static IEnumerable<Histogram> FromQueryRows(IEnumerable<SingleColumnHistogram.Result> queryResults) =>
            from row in queryResults
            where row.HasValue
            group row by row.BucketSize into bucketGroup
            let bucketSize = new BucketSize(bucketGroup.Key)
            select new Histogram(
                bucketSize,
                bucketGroup.Select(b =>
                    new HistogramBucket(
                        (decimal)b.LowerBound,
                        bucketSize,
                        NoisyCount.FromCountableRow(b))));
    }
}