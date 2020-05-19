namespace Explorer.Components
{
    using System.Collections.Generic;

    using Explorer.Metrics;

    public class HistogramPublisher : PublisherComponent<NumericHistogramComponent.Result>
    {
        public HistogramPublisher(ResultProvider<NumericHistogramComponent.Result> resultProvider)
            : base(resultProvider)
        {
        }

        public override IEnumerable<ExploreMetric> YieldMetrics(NumericHistogramComponent.Result result)
        {
            yield return new UntypedMetric("histogram.buckets", result.Histogram.Buckets.Values);
            yield return new UntypedMetric("histogram.suppressed_count", result.ValueCounts.SuppressedCount);
            yield return new UntypedMetric("histogram.suppressed_ratio", result.ValueCounts.SuppressedCountRatio);
            yield return new UntypedMetric("histogram.value_counts", result.ValueCounts);
        }
    }
}