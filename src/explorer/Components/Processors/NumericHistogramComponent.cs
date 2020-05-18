namespace Explorer.Components
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using Diffix;
    using Explorer.Common;
    using Explorer.Queries;

    public class NumericHistogramComponent :
        ExplorerComponent<NumericHistogramComponent.Result>
    {
        private const long ValuesPerBucketTarget = 20;
        private readonly DConnection conn;
        private readonly ExplorerContext ctx;
        private readonly ResultProvider<SimpleStats<double>.Result> statsResultProvider;

        public NumericHistogramComponent(
            DConnection conn,
            ExplorerContext ctx,
            ResultProvider<SimpleStats<double>.Result> statsResultProvider)
        {
            this.conn = conn;
            this.ctx = ctx;
            this.statsResultProvider = statsResultProvider;
        }

        protected async override Task<Result> Explore()
        {
            var stats = await statsResultProvider.ResultAsync;

            var bucketsToSample = BucketUtils.EstimateBucketResolutions(
                stats.Count, stats.Min, stats.Max, ValuesPerBucketTarget);

            var histogramQ = await conn.Exec(new SingleColumnHistogram(ctx.Table, ctx.Column, bucketsToSample));

            var histograms = Histogram.FromQueryRows(histogramQ.Rows);

            var valueCounts = histogramQ.Rows
                .GroupBy(
                    row => row.BucketSize,
                    (bs, rows) => (BucketSize: new BucketSize(bs), Rows: ValueCounts.Compute(rows)));

            var results = valueCounts.Join(
                histograms,
                v => v.BucketSize.SnappedSize,
                h => h.BucketSize.SnappedSize,
                (v, h) => new Result(v.Rows, h));

            return results
                .OrderBy(h => h.BucketSize)
                .ThenBy(h => h.ValueCounts.SuppressedCount)
                .First();
        }

        public class Result
        {
            internal Result(ValueCounts valueCounts, Histogram histogram)
            {
                ValueCounts = valueCounts;
                Histogram = histogram;
            }

            public BucketSize BucketSize => Histogram.BucketSize;

            public ValueCounts ValueCounts { get; }

            public Histogram Histogram { get; }
        }
    }
}