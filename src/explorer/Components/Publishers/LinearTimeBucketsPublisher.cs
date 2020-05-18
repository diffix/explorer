namespace Explorer.Components
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Explorer.Common;
    using Explorer.Metrics;

    public class LinearTimeBucketsPublisher : PublisherComponent<LinearTimeBuckets.Result>
    {
        public LinearTimeBucketsPublisher(ResultProvider<LinearTimeBuckets.Result> resultProvider)
        : base(resultProvider)
        {
        }

        public override IEnumerable<ExploreMetric> YieldMetrics(LinearTimeBuckets.Result result)
        {
            foreach (var (valueCount, row) in result.ValueCounts.Zip(result.Rows))
            {
                yield return new UntypedMetric(
                    name: $"dates_linear.{row.Key}",
                    metric: MetricBlob<DateTime>(valueCount.TotalCount, valueCount.SuppressedCount, row.Select(_ => _)));
            }
        }

        private static object MetricBlob<T>(
            long total,
            long suppressed,
            IEnumerable<GroupingSetsResult<T>> valueCounts)
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