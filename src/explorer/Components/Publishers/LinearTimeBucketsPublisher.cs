namespace Explorer.Components
{
    using System.Collections.Generic;
    using System.Linq;

    using Explorer.Common;
    using Explorer.Metrics;

    public class LinearTimeBucketsPublisher : PublisherComponent
    {
        private readonly ResultProvider<LinearTimeBuckets.Result> resultProvider;

        public LinearTimeBucketsPublisher(
            ResultProvider<LinearTimeBuckets.Result> resultProvider)
        {
            this.resultProvider = resultProvider;
        }

        public override async IAsyncEnumerable<ExploreMetric> YieldMetrics()
        {
            var result = await resultProvider.ResultAsync;

            foreach (var (valueCount, row) in result.ValueCounts.Zip(result.Rows))
            {
                yield return new UntypedMetric(
                    name: $"dates_linear.{row.Key}",
                    metric: MetricBlob(valueCount.TotalCount, valueCount.SuppressedCount, row.Select(_ => _)));
            }
        }

        private static object MetricBlob<T>(
            long total,
            long suppressed,
            IEnumerable<ValueWithCount<T>> valueCounts)
        {
            return new
            {
                Total = total,
                Suppressed = suppressed,
                Counts =
                    from row in valueCounts
                    where row.HasValue
                    orderby row.Value ascending
                    select new
                    {
                        row.Value,
                        row.Count,
                        row.CountNoise,
                    },
            };
        }
    }
}