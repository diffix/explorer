namespace Explorer.Components
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using Diffix;
    using Explorer.Common;
    using Explorer.Components.ResultTypes;
    using Explorer.Queries;
    using Microsoft.Extensions.Logging;

    public class NumericHistogramComponent :
        ExplorerComponent<List<HistogramWithCounts>>
    {
        private const long ValuesPerBucketTarget = 20;

        private readonly ResultProvider<SimpleStats<double>.Result> statsResultProvider;
        private readonly ResultProvider<DistinctValuesComponent.Result> distinctValuesProvider;

        public NumericHistogramComponent(
            ResultProvider<SimpleStats<double>.Result> statsResultProvider,
            ResultProvider<DistinctValuesComponent.Result> distinctValuesProvider,
            Logger<NumericHistogramComponent> logger)
        {
            this.statsResultProvider = statsResultProvider;
            this.distinctValuesProvider = distinctValuesProvider;
            Logger = logger;
        }

        private Logger<NumericHistogramComponent> Logger { get; }

        protected async override Task<List<HistogramWithCounts>?> Explore()
        {
            var stats = await statsResultProvider.ResultAsync;
            if (stats == null)
            {
                return null;
            }

            var (minBound, maxBound) = (stats.Min, stats.Max);
            if (!minBound.HasValue || !maxBound.HasValue)
            {
                var distincts = await distinctValuesProvider.ResultAsync;
                if (distincts == null || distincts.ValueCounts.NonSuppressedNonNullCount == 0)
                {
                    return null;
                }

                var values = distincts.DistinctRows.Where(row => row.HasValue).Select(row => row.Value.GetDouble());
                minBound ??= values.Min();
                maxBound ??= values.Max();
            }

            if (!minBound.HasValue || !maxBound.HasValue || minBound == maxBound)
            {
                Logger.LogWarning("Unable to calculate suitable bounds for numerical column {Context.Column}.");

                return null;
            }

            var bucketsToSample = BucketUtils.EstimateBucketResolutions(
                stats.Count,
                (double)minBound,
                (double)maxBound,
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