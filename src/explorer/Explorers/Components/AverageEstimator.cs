namespace Explorer.Explorers.Components
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Diffix;
    using Explorer.Common;
    using Explorer.Explorers.Metrics;

    public class AverageEstimator :
        ExplorerComponent<AverageEstimator.Result>
    {
        private readonly ResultProvider<NumericHistogramComponent.Result> histogramResultProvider;

        public AverageEstimator(ResultProvider<NumericHistogramComponent.Result> histogramResultProvider)
        {
            this.histogramResultProvider = histogramResultProvider;
        }

        protected override async Task<Result> Explore()
        {
            var histogram = await histogramResultProvider.ResultAsync;

            var averageEstimate = await Task.Run(() =>
            {
                var sum = histogram.Buckets
                        .Where(b => b.HasValue)
                        .Sum(bucket => bucket.Count * ((decimal)bucket.LowerBound.Value + (histogram.BucketSize / 2)));
                return sum / histogram.ValueCounts.NonSuppressedNonNullCount;
            });

            return new Result(averageEstimate);
        }

        public class Result : MetricsProvider
        {
            public Result(decimal value)
            {
                Value = value;
            }

            public decimal Value { get; }

            public IEnumerable<ExploreMetric> Metrics()
            {
                yield return new UntypedMetric(name: "average_estimate", metric: decimal.Round(Value, 4));
            }
        }
    }
}