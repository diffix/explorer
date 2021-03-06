namespace Explorer.Components
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using Diffix;
    using Explorer.Common;
    using Explorer.Metrics;
    using Explorer.Queries;

    public class CyclicalTimeBuckets : ExplorerComponent<CyclicalTimeBuckets.Result>, PublisherComponent
    {
        public async IAsyncEnumerable<ExploreMetric> YieldMetrics()
        {
            var result = await ResultAsync;
            if (result == null)
            {
                yield break;
            }

            foreach (var m in TimeUtilities.YieldMetrics<Result, int>("dates_cyclical", result))
            {
                yield return m;
            }
        }

        protected override async Task<Result?> Explore()
        {
            var queryResult = await Context.Exec(new CyclicalDatetimes(Context.ColumnInfo.Type));

            var groupings = ProcessCyclicalBuckets(queryResult.Rows).ToList();

            return new Result(
                groupings.Select(g => g.Item1),
                groupings.Select(g => g.Item2));
        }

        private static IEnumerable<(ValueCounts, IGrouping<string, GroupingSetsResult<int>>)> ProcessCyclicalBuckets(
            IEnumerable<GroupingSetsResult<int>> queryResult)
        {
            // Always skip years because they are never cyclical
            var skip = true;

            foreach (var group in TimeUtilities.GroupByLabel(queryResult.OrderBy(r => r.GroupingIndex)))
            {
                if (skip)
                {
                    var distinctValueCount = group.Count(row => row.HasValue);

                    // If we haven't completed at least two full cycles of the current time period,
                    // skip the next time period.
                    // Once we have a cycle, we include all the remaining time periods (ie. we stop skipping).
                    skip = (group.Key, distinctValueCount) switch
                    {
                        // Quarters cycle every year
                        ("year", var count) when count > 2 => false,

                        // Months cycle every four quarters
                        ("quarter", var count) when count > 8 => false,

                        // Days cycle every month
                        ("month", var count) when count > 2 => false,

                        // Weekdays cycle every 7 days
                        ("day", var count) when count > 14 => false,

                        // Hours cycle every weekday
                        ("weekday", var count) when count > 2 => false,

                        // Minutes cycle every hour
                        ("hour", var count) when count > 2 => false,

                        // Seconds cycle every Minute
                        ("minute", var count) when count > 2 => false,

                        ("second", var count) when count > 2 => false,
                        _ => true,
                    };
                }
                else
                {
                    var counts = ValueCounts.Compute(group);
                    if (TimeUtilities.TooManySuppressedValues(counts))
                    {
                        // If a lot of rows are suppressed, stop.
                        break;
                    }

                    yield return (counts, group);
                }
            }
        }

        public class Result : TimeUtilities.GenericResult<int>
        {
            public Result(
                IEnumerable<ValueCounts> valueCounts,
                IEnumerable<IGrouping<string, GroupingSetsResult<int>>> rows)
            : base(valueCounts, rows)
            {
            }
        }
    }
}