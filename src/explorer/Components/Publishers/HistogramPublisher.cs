namespace Explorer.Components
{
    using System.Collections.Generic;
    using System.Linq;

    using Explorer.Metrics;

    public class HistogramPublisher : PublisherComponent<NumericHistogramComponent.Result>
    {
        public HistogramPublisher(ResultProvider<NumericHistogramComponent.Result> resultProvider)
            : base(resultProvider)
        {
        }

        public override IEnumerable<ExploreMetric> YieldMetrics(NumericHistogramComponent.Result result)
        {
            var buckets = result.Buckets
                    .Where(b => b.HasValue)
                    .Select(b => new
                    {
                        result.BucketSize,
                        b.LowerBound,
                        b.Count,
                    });

            yield return new UntypedMetric("histogram.buckets", buckets);
            yield return new UntypedMetric("histogram.suppressed_count", result.ValueCounts.SuppressedCount);
            yield return new UntypedMetric("histogram.suppressed_ratio", result.ValueCounts.SuppressedCountRatio);
            yield return new UntypedMetric("histogram.value_counts", result.ValueCounts);
        }
    }
}