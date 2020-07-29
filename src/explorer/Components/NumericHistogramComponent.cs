namespace Explorer.Components
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using Diffix;
    using Explorer.Common;
    using Explorer.Components.ResultTypes;
    using Explorer.Queries;

    public class NumericHistogramComponent :
        ExplorerComponent<List<HistogramWithCounts>>
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

        protected async override Task<List<HistogramWithCounts>> Explore()
        {
            var stats = await statsResultProvider.ResultAsync;

            var bucketsToSample = BucketUtils.EstimateBucketResolutions(
                stats.Count,
                stats.Min,
                stats.Max,
                ValuesPerBucketTarget,
                isIntegerColumn: ctx.ColumnInfo.Type == DValueType.Integer);

            var histogramQ = await ctx.Exec(new SingleColumnHistogram(ctx.Table, ctx.Column, bucketsToSample));

            var histograms = Histogram.FromQueryRows(histogramQ.Rows);

            var valueCounts = histogramQ.Rows
                .GroupBy(
                    row => row.BucketSize,
                    (bs, rows) => (BucketSize: new BucketSize(bs), Rows: ValueCounts.Compute(rows)));

            return valueCounts
                .Join(
                    histograms,
                    v => v.BucketSize.SnappedSize,
                    h => h.BucketSize.SnappedSize,
                    (v, h) => new HistogramWithCounts(v.Rows, h))
                .ToList();
        }
    }
}