namespace Explorer.Components
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using Explorer.Common;
    using Explorer.Components.ResultTypes;
    using Explorer.Metrics;

    public class AverageEstimator :
        ExplorerComponent<AverageEstimator.Result>, PublisherComponent
    {
        private readonly ResultProvider<HistogramWithCounts> histogramResultProvider;

        public AverageEstimator(ResultProvider<HistogramWithCounts> histogramResultProvider)
        {
            this.histogramResultProvider = histogramResultProvider;
        }

        public static Task<decimal> EstimateAverage(HistogramWithCounts hwc) =>
            EstimateAverage(hwc.Histogram);

        public static Task<decimal> EstimateAverage(Histogram histogram) => Task.Run(() =>
        {
            (decimal Sum, long Total) sumzero = (0M, 0L);
            var (sum, total) = histogram.Buckets.Values
                .Aggregate(
                    sumzero,
                    (sums, bucket) => (
                            sums.Sum + (bucket.Count * (bucket.LowerBound + (histogram.BucketSize.SnappedSize / 2))),
                            sums.Total + bucket.Count));

            return sum / total;
        });

        public async IAsyncEnumerable<ExploreMetric> YieldMetrics()
        {
            const int Precision = 6;
            var result = await ResultAsync;
            if (result == null)
            {
                yield break;
            }

            yield return new UntypedMetric(name: "average_estimate", metric: decimal.Round(result.Value, Precision));
        }

        protected override async Task<Result?> Explore()
        {
            var histogramResult = await histogramResultProvider.ResultAsync;
            if (histogramResult == null)
            {
                return null;
            }

            var average = await EstimateAverage(histogramResult);
            return new Result(average);
        }

        public class Result
        {
            public Result(decimal value)
            {
                Value = value;
            }

            public decimal Value { get; }
        }
    }
}