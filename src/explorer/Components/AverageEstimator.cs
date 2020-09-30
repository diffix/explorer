namespace Explorer.Components
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using Explorer.Common;
    using Explorer.Metrics;

    public class AverageEstimator : ExplorerComponentBase, PublisherComponent
    {
        private const int Precision = 6;

        private readonly ResultProvider<Histogram> histogramResultProvider;

        public AverageEstimator(ResultProvider<Histogram> histogramResultProvider)
        {
            this.histogramResultProvider = histogramResultProvider;
        }

        public static Task<decimal> EstimateAverage(Histogram histogram) => Task.Run(() =>
        {
            var halfBucketSize = histogram.SnappedBucketSize / 2;
            var (sum, total) = histogram.Buckets.Aggregate(
                    (Sum: 0M, Total: 0L),
                    (sums, bucket) => (
                        sums.Sum + (bucket.Count * (bucket.LowerBound + halfBucketSize)),
                        sums.Total + bucket.Count));
            return sum / total;
        });

        public async IAsyncEnumerable<ExploreMetric> YieldMetrics()
        {
            var histogramResult = await histogramResultProvider.ResultAsync;
            if (histogramResult == null)
            {
                yield break;
            }

            var average = await EstimateAverage(histogramResult);
            yield return ExploreMetric.Create(MetricDefinitions.AverageEstimate, decimal.Round(average, Precision));
        }
    }
}