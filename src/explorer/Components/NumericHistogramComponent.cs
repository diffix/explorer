namespace Explorer.Components
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using Diffix;
    using Explorer.Common;
    using Explorer.Metrics;
    using Explorer.Queries;

    public class NumericHistogramComponent :
        ExplorerComponent<NumericHistogramComponent.Result>, PublisherComponent
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

        public async IAsyncEnumerable<ExploreMetric> YieldMetrics()
        {
            var result = await ResultAsync;

            yield return new UntypedMetric("histogram.buckets", result.Histogram.Buckets.Values.Select(b => new
            {
                BucketSize = b.BucketSize.SnappedSize,
                b.LowerBound,
                b.Count,
                b.CountNoise,
            }));
            yield return new UntypedMetric("histogram.suppressed_count", result.ValueCounts.SuppressedCount);
            yield return new UntypedMetric("histogram.suppressed_ratio", result.ValueCounts.SuppressedCountRatio);
            yield return new UntypedMetric("histogram.value_counts", result.ValueCounts);
        }

        protected async override Task<Result> Explore()
        {
            var stats = await statsResultProvider.ResultAsync;

            var bucketsToSample = BucketUtils.EstimateBucketResolutions(
                stats.Count,
                stats.Min,
                stats.Max,
                ValuesPerBucketTarget,
                isIntegerColumn: ctx.ColumnInfo.Type == DValueType.Integer);

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
                .OrderBy(h => h.BucketSize.SnappedSize)
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