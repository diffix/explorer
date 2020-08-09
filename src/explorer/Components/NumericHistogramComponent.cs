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

        private readonly ResultProvider<SimpleStats<double>.Result> statsResultProvider;

        public NumericHistogramComponent(ResultProvider<SimpleStats<double>.Result> statsResultProvider)
        {
            this.statsResultProvider = statsResultProvider;
        }

        protected async override Task<List<HistogramWithCounts>?> Explore()
        {
            var stats = await statsResultProvider.ResultAsync;
            if (stats == null)
            {
                return null;
            }

            var bucketsToSample = BucketUtils.EstimateBucketResolutions(
                stats.Count,
                stats.Min,
                stats.Max,
                ValuesPerBucketTarget,
                isIntegerColumn: Context.ColumnInfo.Type == DValueType.Integer);

            var histogramQ = await Context.Exec(new SingleColumnHistogram(bucketsToSample));

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