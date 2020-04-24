namespace Explorer.Explorers.Components
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Diffix;
    using Explorer.Common;
    using Explorer.Explorers.Metrics;

    internal class AverageEstimator : ExplorerComponent<AverageEstimator.Result>, DependsOn<NumericHistogramComponent.Result>
    {
        private ExplorerComponent<NumericHistogramComponent.Result>? histogramComponent;

        public AverageEstimator(DConnection conn, ExplorerContext ctx)
        : base(conn, ctx)
        {
        }

        public void LinkToSourceComponent(ExplorerComponent<NumericHistogramComponent.Result> component)
        {
            histogramComponent = component;
        }

        protected override async Task<Result> Explore()
        {
            histogramComponent ??= new NumericHistogramComponent(Conn, Ctx);

            var histogram = await histogramComponent.ResultAsync;

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