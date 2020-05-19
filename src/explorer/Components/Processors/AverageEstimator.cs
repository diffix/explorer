namespace Explorer.Components
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using Explorer.Common;
    using Explorer.Metrics;

    public class AverageEstimator :
        ExplorerComponent<AverageEstimator.Result>, PublisherComponent
    {
        private readonly ResultProvider<NumericHistogramComponent.Result> histogramResultProvider;

        public AverageEstimator(ResultProvider<NumericHistogramComponent.Result> histogramResultProvider)
        {
            this.histogramResultProvider = histogramResultProvider;
        }

        public static Task<decimal> EstimateAverage(NumericHistogramComponent.Result result) =>
            EstimateAverage(result.Histogram);

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

            yield return new UntypedMetric(name: "average_estimate", metric: decimal.Round(result.Value, Precision));
        }

        protected override async Task<Result> Explore() =>
            new Result(await EstimateAverage(await histogramResultProvider.ResultAsync));

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