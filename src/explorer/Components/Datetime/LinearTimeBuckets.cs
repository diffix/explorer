namespace Explorer.Components
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using Explorer.Common;
    using Explorer.Common.Utils;
    using Explorer.Metrics;
    using Explorer.Queries;

    public class LinearTimeBuckets : ExplorerComponent<LinearTimeBuckets.Result>, PublisherComponent
    {
        private static readonly Dictionary<string, MetricDefinition<DateTimeMetric<DateTime>>> DatesLinearMetrics =
            new List<MetricDefinition<DateTimeMetric<DateTime>>>
            {
                MetricDefinitions.DatesLinearSecond,
                MetricDefinitions.DatesLinearMinute,
                MetricDefinitions.DatesLinearHour,
                MetricDefinitions.DatesLinearDay,
                MetricDefinitions.DatesLinearMonth,
                MetricDefinitions.DatesLinearQuarter,
                MetricDefinitions.DatesLinearYear,
            }
            .ToDictionary(item => item.Name);

        public async IAsyncEnumerable<ExploreMetric> YieldMetrics()
        {
            var result = await ResultAsync;
            if (result == null)
            {
                yield break;
            }

            foreach (var m in TimeUtilities.YieldMetrics("datesLinear", result, DatesLinearMetrics))
            {
                yield return m;
            }
        }

        protected override async Task<Result?> Explore()
        {
            var queryResult = await Context.Exec(new BucketedDatetimes(Context.ColumnInfo.Type));

            var groupings = ProcessLinearBuckets(queryResult.Rows).ToList();

            return new Result(
                groupings.Select(g => g.Item1),
                groupings.Select(g => g.Item2));
        }

        private static IEnumerable<(ValueCounts, IGrouping<string, GroupingSetsResult<DateTime>>)> ProcessLinearBuckets(
            IEnumerable<GroupingSetsResult<DateTime>> queryResult)
        {
            foreach (var group in TimeUtilities.GroupByLabel(queryResult))
            {
                var counts = ValueCounts.Compute(group);
                if (TimeUtilities.TooManySuppressedValues(counts))
                {
                    break;
                }

                yield return (counts, group);
            }
        }

        public class Result : TimeUtilities.GenericResult<DateTime>
        {
            public Result(
                IEnumerable<ValueCounts> valueCounts,
                IEnumerable<IGrouping<string, GroupingSetsResult<DateTime>>> rows)
            : base(valueCounts, rows)
            {
            }
        }
    }
}