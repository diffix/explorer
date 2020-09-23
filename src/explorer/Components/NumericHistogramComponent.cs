namespace Explorer.Components
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using Diffix;
    using Explorer.Common;
    using Explorer.Common.Utils;
    using Explorer.Metrics;
    using Explorer.Queries;

    public class NumericHistogramComponent :
        ExplorerComponent<List<Histogram>>
    {
        private const long ValuesPerBucketTarget = 20;

        private readonly ResultProvider<SimpleStats<double>.Result> statsResultProvider;

        public NumericHistogramComponent(ResultProvider<SimpleStats<double>.Result> statsResultProvider)
        {
            this.statsResultProvider = statsResultProvider;
        }

        protected async override Task<List<Histogram>?> Explore()
        {
            var stats = await statsResultProvider.ResultAsync;
            if (stats == null)
            {
                return null;
            }
            if (stats.Min == null || stats.Max == null)
            {
                return null;
            }

            var bucketsToSample = BucketUtils.EstimateBucketResolutions(
                    stats.Count,
                    stats.Min.Value,
                    stats.Max.Value,
                    ValuesPerBucketTarget,
                    isIntegerColumn: Context.ColumnInfo.Type == DValueType.Integer);

            var histogramResult = await Context.Exec(new SingleColumnHistogram(bucketsToSample));

            var histograms = histogramResult.Rows
                    .Where(row => row.HasValue)
                    .GroupBy(row => row.BucketSize, (bucketSize, rows) =>
                    {
                        var bs = new BucketSize(bucketSize);
                        return (
                            SnappedBucketSize: bs.SnappedSize,
                            Buckets: rows.Select(b =>
                                new HistogramBucket((decimal)b.LowerBound, bs, NoisyCount.FromCountableRow(b))));
                    });

            var valueCounts = histogramResult.Rows
                    .GroupBy(row => row.BucketSize, (bs, rows) =>
                        (BucketSize: new BucketSize(bs), Rows: ValueCounts.Compute(rows)));

            return valueCounts
                    .Join(
                        histograms,
                        v => v.BucketSize.SnappedSize,
                        h => h.SnappedBucketSize,
                        (v, h) => new Histogram(h.Buckets, v.Rows))
                    .ToList();
        }
    }
}