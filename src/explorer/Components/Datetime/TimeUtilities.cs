namespace Explorer.Components
{
    using System.Collections.Generic;
    using System.Globalization;
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

        internal static IEnumerable<ExploreMetric> YieldMetrics<TResult, T>(
            string metricNamePrefix,
            TResult result,
            Dictionary<string, MetricDefinition<DateTimeMetric<T>>> metricDefinitions)
        where TResult : GenericResult<T>
        {
            foreach (var (valueCount, row) in result.ValueCounts.Zip(result.Rows))
            {
                var counts = row
                        .Where(item => item.HasValue)
                        .OrderBy(item => item.GroupingIndex)
                        .Select(item => new ValueWithCountAndNoise<T>(item.Value, item.Count, item.CountNoise))
                        .ToList();
                var metricName = metricNamePrefix + CultureInfo.InvariantCulture.TextInfo.ToTitleCase(row.Key);
                yield return ExploreMetric.Create(
                    metricDefinitions[metricName],
                    new DateTimeMetric<T>(valueCount.TotalCount, valueCount.SuppressedCount, counts));
            }
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