namespace Explorer.Explorers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using Diffix;
    using Explorer.Common;
    using Explorer.Queries;

    internal class DatetimeColumnExplorer : ExplorerBase<ColumnExplorerContext>
    {
        // TODO: The following should be configuration items (?)
        private const double SuppressedRatioThreshold = 0.1;

        public override async Task Explore(DConnection conn, ColumnExplorerContext ctx)
        {
            var statsQ = await conn.Exec<NumericColumnStats.Result<DateTime>>(
                new NumericColumnStats(ctx.Table, ctx.Column));

            var stats = statsQ.Rows.Single();

            PublishMetric(new UntypedMetric(name: "naive_min", metric: stats.Min));
            PublishMetric(new UntypedMetric(name: "naive_max", metric: stats.Max));

            var distinctValueQ = await conn.Exec(
                new DistinctColumnValues(ctx.Table, ctx.Column));

            var counts = ValueCounts.Compute(distinctValueQ.Rows);

            if (counts.TotalCount == 0)
            {
                throw new Exception(
                    $"Total value count for {ctx.Table}, {ctx.Column} is zero.");
            }

            if (counts.SuppressedCountRatio < SuppressedRatioThreshold)
            {
                // Only few of the values are suppressed. This means the data is already well-segmented and can be
                // considered categorical or quasi-categorical.
                var distinctValues =
                    from row in distinctValueQ.Rows
                    where row.HasValue
                    orderby row.Count descending
                    select new
                    {
                        row.Value,
                        row.Count,
                    };

                PublishMetric(new UntypedMetric(name: "distinct.values", metric: distinctValues));
                PublishMetric(new UntypedMetric(name: "distinct.suppressed_count", metric: counts.SuppressedCount));
            }

            await Task.WhenAll(
                LinearBuckets(conn, ctx),
                CyclicalBuckets(conn, ctx));

            // Other metrics?
            // Median
            // Average
        }

        private static object DatetimeMetric<T>(
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

        private async Task LinearBuckets(DConnection conn, ColumnExplorerContext ctx)
        {
            var queryResult = await conn.Exec(
                new BucketedDatetimes(ctx.Table, ctx.Column, ctx.ColumnType));

            await Task.Run(() => ProcessLinearBuckets(conn, queryResult.Rows));
        }

        private async Task CyclicalBuckets(DConnection conn, ColumnExplorerContext ctx)
        {
            var queryResult = await conn.Exec(
                new CyclicalDatetimes(ctx.Table, ctx.Column, ctx.ColumnType));

            await Task.Run(() => ProcessCyclicalBuckets(conn, queryResult.Rows));
        }

        private void ProcessLinearBuckets(DConnection conn, IEnumerable<GroupingSetsResult<DateTime>> queryResult)
        {
            foreach (var group in GroupByLabel(queryResult))
            {
                conn.ThrowIfCancellationRequested();

                var counts = ValueCounts.Compute(group);
                if (counts.SuppressedCountRatio > SuppressedRatioThreshold)
                {
                    break;
                }

                var label = group.Key;
                PublishMetric(new UntypedMetric(name: $"dates_linear.{label}", metric: DatetimeMetric(
                    counts.TotalCount, counts.SuppressedCount, group)));
            }
        }

        private void ProcessCyclicalBuckets(DConnection conn, IEnumerable<GroupingSetsResult<int>> queryResult)
        {
            var includeRest = false;
            foreach (var group in GroupByLabel(queryResult))
            {
                conn.ThrowIfCancellationRequested();

                var label = group.Key;

                if (!includeRest)
                {
                    var distinctValueCount = group.Count(row => row.HasValue);

                    includeRest = (label, distinctValueCount) switch
                    {
                        ("quarter", var count) when count > 4 => true,
                        ("day", var count) when count > 7 => true,
                        (_, var count) when count > 1 => true,
                        _ => false,
                    };

                    continue;
                }

                var counts = ValueCounts.Compute(group);
                if (counts.SuppressedCountRatio > SuppressedRatioThreshold)
                {
                    break;
                }

                PublishMetric(new UntypedMetric(name: $"dates_cyclical.{label}", metric: DatetimeMetric(
                    counts.TotalCount, counts.SuppressedCount, group)));
            }
        }

        private IEnumerable<IGrouping<string, GroupingSetsResult<T>>> GroupByLabel<T>(
            IEnumerable<GroupingSetsResult<T>> queryResult)
        {
            return queryResult.GroupBy(row => row.GroupingLabel);
        }
    }
}
