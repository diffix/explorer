namespace Explorer.Components
{
    using System.Collections.Generic;
    using System.Linq;

    using Explorer.Common;
    using Explorer.Common.Utils;
    using Explorer.Metrics;

    public static class TimeUtilities
    {
        private const double SuppressedRatioThreshold = 0.2;

        public static IEnumerable<IGrouping<string, GroupingSetsResult<T>>> GroupByLabel<T>(
            IEnumerable<GroupingSetsResult<T>> queryResult)
        {
            return queryResult.GroupBy(row => row.GroupingLabel);
        }

        internal static bool TooManySuppressedValues(ValueCounts counts)
            => counts.SuppressedCountRatio > SuppressedRatioThreshold;

        internal static IEnumerable<ExploreMetric> YieldMetrics<TResult, T>(string metricName, TResult result)
        where TResult : GenericResult<T>
        {
            foreach (var (valueCount, row) in result.ValueCounts.Zip(result.Rows))
            {
                yield return new UntypedMetric(
                    name: $"{metricName}_{row.Key}",
                    metric: MetricBlob(
                        valueCount.TotalCount,
                        valueCount.SuppressedCount,
                        row.Select(_ => _)));
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
                Counts = valueCounts
                    .Where(row => row.HasValue)
                    .OrderBy(row => row.GroupingIndex)
                    .Select(row => new
                    {
                        row.Value,
                        row.Count,
                        row.CountNoise,
                    })
                    .ToList(),
            };
        }

        public class GenericResult<T>
        {
            public GenericResult(
                IEnumerable<ValueCounts> valueCounts,
                IEnumerable<IGrouping<string, GroupingSetsResult<T>>> rows)
            {
                ValueCounts = valueCounts;
                Rows = rows;
            }

            public IEnumerable<ValueCounts> ValueCounts { get; }

            public IEnumerable<IGrouping<string, GroupingSetsResult<T>>> Rows { get; }
        }
    }
}