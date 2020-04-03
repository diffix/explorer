namespace Explorer.Explorers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using Diffix;
    using Explorer.Common;
    using Explorer.Queries;

    internal class DatetimeColumnExplorer : ExplorerBase
    {
        // TODO: The following should be configuration items (?)
        private const double SuppressedRatioThreshold = 0.1;

        public DatetimeColumnExplorer(
            DQueryResolver queryResolver,
            string tableName,
            string columnName,
            DValueType columnType = DValueType.Datetime)
            : base(queryResolver)
        {
            TableName = tableName;
            ColumnName = columnName;
            ColumnType = columnType;
        }

        private string TableName { get; }

        private string ColumnName { get; }

        private DValueType ColumnType { get; }

        public override async Task Explore()
        {
            var statsQ = await ResolveQuery<NumericColumnStats.Result<DateTime>>(
                new NumericColumnStats(TableName, ColumnName));

            var stats = statsQ.Rows.Single();

            PublishMetric(new UntypedMetric(name: "naive_min", metric: stats.Min));
            PublishMetric(new UntypedMetric(name: "naive_max", metric: stats.Max));

            var distinctValueQ = await ResolveQuery(
                new DistinctColumnValues(TableName, ColumnName));

            var counts = ValueCounts.Compute(distinctValueQ.Rows);

            if (counts.TotalCount == 0)
            {
                throw new Exception(
                    $"Total value count for {TableName}, {ColumnName} is zero.");
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
                LinearBuckets(),
                CyclicalBuckets());

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

        private async Task LinearBuckets()
        {
            var queryResult = await ResolveQuery(
                new BucketedDatetimes(TableName, ColumnName, ColumnType));

            await Task.Run(() => ProcessLinearBuckets(queryResult.Rows));
        }

        private async Task CyclicalBuckets()
        {
            var queryResult = await ResolveQuery(
                new CyclicalDatetimes(TableName, ColumnName, ColumnType));

            await Task.Run(() => ProcessCyclicalBuckets(queryResult.Rows));
        }

        private void ProcessLinearBuckets(IEnumerable<GroupingSetsResult<DateTime>> queryResult)
        {
            foreach (var group in GroupByLabel(queryResult))
            {
                ThrowIfCancellationRequested();

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

        private void ProcessCyclicalBuckets(IEnumerable<GroupingSetsResult<int>> queryResult)
        {
            var includeRest = false;
            foreach (var group in GroupByLabel(queryResult))
            {
                ThrowIfCancellationRequested();

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
