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
        private readonly ResultProvider<Histogram> histogramResultProvider;

        public AverageEstimator(ResultProvider<Histogram> histogramResultProvider)
        {
            this.histogramResultProvider = histogramResultProvider;
        }

        public static Task<decimal> EstimateAverage(Histogram histogram) => Task.Run(() =>
        {
            var halfBucketSize = histogram.GetSnappedBucketSize() / 2;
            var (sum, total) = histogram.Buckets.Aggregate(
                    (Sum: 0M, Total: 0L),
                    (sums, bucket) => (
                        sums.Sum + (bucket.Count * (bucket.LowerBound + halfBucketSize)),
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

            yield return ExploreMetric.Create(MetricDefinitions.AverageEstimate, decimal.Round(result.Value, Precision));

            // alternative syntax:
            // yield return AverageEstimate.Create(decimal.Round(result.Value, Precision));
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