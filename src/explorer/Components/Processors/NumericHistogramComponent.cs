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

            var histograms = histogramQ.Rows
                .GroupBy(
                    row => row.GroupingLabel,
                    (bucketSize, buckets) => new Result(
                        bucketSize,
                        ValueCounts.Compute(buckets),
                        buckets.Where(b => b.HasValue).OrderBy(b => b.LowerBound)));

            return histograms
                .OrderBy(h => h.BucketSize)
                .ThenBy(h => h.ValueCounts.SuppressedCount)
                .First();
        }

        public class Result
        {
            internal Result(
                decimal bucketSize,
                ValueCounts valueCounts,
                IEnumerable<SingleColumnHistogram.Result> buckets)
            {
                BucketSize = bucketSize;
                ValueCounts = valueCounts;
                Buckets = buckets;
            }

            public decimal BucketSize { get; set; }

            public ValueCounts ValueCounts { get; set; }

            public IEnumerable<SingleColumnHistogram.Result> Buckets { get; set; }
        }
    }
}