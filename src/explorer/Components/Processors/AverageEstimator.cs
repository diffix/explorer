namespace Explorer.Components
{
    using System.Linq;
    using System.Threading.Tasks;

    using Explorer.Common;

    public class AverageEstimator :
        ExplorerComponent<AverageEstimator.Result>
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