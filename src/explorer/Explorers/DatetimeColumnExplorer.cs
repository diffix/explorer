namespace Explorer.Explorers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using Diffix;
    using Explorer.Common;
    using Explorer.Queries;

    internal class DatetimeColumnExplorer
    {
        // TODO: The following should be configuration items (?)
        private const double SuppressedRatioThreshold = 0.1;

        public async Task Explore(DConnection conn, ExplorerContext ctx)
        {
            var statsQ = await conn.Exec<NumericColumnStats.Result<DateTime>>(
                new NumericColumnStats(ctx.Table, ctx.Column));

            var stats = statsQ.Rows.Single();

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
            IEnumerable<GroupingSetsResult<T>> rows)
        {
            return new
            {
                Total = total,
                Suppressed = suppressed,
                Counts =
                    from row in rows
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

        private async Task LinearBuckets(DConnection conn, ExplorerContext ctx)
        {
            var queryResult = await conn.Exec(
                new BucketedDatetimes(ctx.Table, ctx.Column, ctx.ColumnType));

            await Task.Run(() => ProcessLinearBuckets(conn, queryResult.Rows));
        }

        private async Task CyclicalBuckets(DConnection conn, ExplorerContext ctx)
        {
            var queryResult = await conn.Exec(
                new CyclicalDatetimes(ctx.Table, ctx.Column, ctx.ColumnType));

            await Task.Run(() => ProcessCyclicalBuckets(conn, queryResult.Rows));
        }

        private void ProcessLinearBuckets(DConnection conn, IEnumerable<GroupingSetsResult<DateTime>> queryResult)
        {
            foreach (var group in GroupByLabel(queryResult))
            {

                var counts = ValueCounts.Compute(group);
                if (counts.SuppressedCountRatio > SuppressedRatioThreshold)
                {
                    break;
                }

                var label = group.Key;
            }
        }

        private void ProcessCyclicalBuckets(DConnection conn, IEnumerable<GroupingSetsResult<int>> queryResult)
        {
            var includeRest = false;
            foreach (var group in GroupByLabel(queryResult))
            {

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

            }
        }

        private IEnumerable<IGrouping<string, GroupingSetsResult<T>>> GroupByLabel<T>(
            IEnumerable<GroupingSetsResult<T>> queryResult)
        {
            return queryResult.GroupBy(row => row.GroupingLabel);
        }
    }
}
