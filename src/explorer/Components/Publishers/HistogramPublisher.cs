namespace Explorer.Components
{
    using System.Collections.Generic;
    using System.Linq;

    using Explorer.Metrics;

    public class HistogramPublisher : PublisherComponent
    {
        private readonly ResultProvider<NumericHistogramComponent.Result> resultProvider;

        public HistogramPublisher(
            ResultProvider<NumericHistogramComponent.Result> resultProvider)
        {
            this.resultProvider = resultProvider;
        }

        public override async IAsyncEnumerable<ExploreMetric> YieldMetrics()
        {
            var histogramResult = await resultProvider.ResultAsync;

            var buckets = histogramResult.Buckets
                    .Where(b => b.HasValue)
                    .Select(b => new
                    {
                        histogramResult.BucketSize,
                        LowerBound = b.GroupingValue,
                        b.Count,
                    });

            yield return new UntypedMetric("histogram.buckets", buckets);
            yield return new UntypedMetric("histogram.suppressed_count", histogramResult.ValueCounts.SuppressedCount);
            yield return new UntypedMetric("histogram.suppressed_ratio", histogramResult.ValueCounts.SuppressedCountRatio);
            yield return new UntypedMetric("histogram.value_counts", histogramResult.ValueCounts);
        }
    }
}