namespace Explorer
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    using Aircloak.JsonApi.ResponseTypes;

    using Explorer.Diffix.Extensions;
    using Explorer.Diffix.Interfaces;
    using Explorer.Queries;

    internal class DatetimeColumnExplorer : ExplorerBase
    {
        // TODO: The following should be configuration items (?)
        private const double SuppressedRatioThreshold = 0.1;

        public DatetimeColumnExplorer(IQueryResolver queryResolver, string tableName, string columnName)
            : base(queryResolver)
        {
            TableName = tableName;
            ColumnName = columnName;
        }

        private string TableName { get; }

        private string ColumnName { get; }

        public override async Task Explore(CancellationToken cancellationToken)
        {
            var statsQ = (await ResolveQuery<NumericColumnStats.Result<DateTime>>(
                new NumericColumnStats(TableName, ColumnName),
                cancellationToken));

            var stats = statsQ.ResultRows.Single();

            PublishMetric(new UntypedMetric(name: "naive_min", metric: stats.Min));
            PublishMetric(new UntypedMetric(name: "naive_max", metric: stats.Max));

            var distinctValueQ = await ResolveQuery<DistinctColumnValues.Result<DateTime>>(
                new DistinctColumnValues(TableName, ColumnName),
                cancellationToken);

            var suppressedValueCount = distinctValueQ.ResultRows.Sum(row =>
                    row.DistinctData.IsSuppressed ? row.Count : 0);

            var totalValueCount = stats.Count;

            if (totalValueCount == 0)
            {
                throw new Exception(
                    $"Total value count for {TableName}, {ColumnName} is zero.");
            }

            var suppressedValueRatio = (double)suppressedValueCount / totalValueCount;

            if (suppressedValueRatio < SuppressedRatioThreshold)
            {
                // Only few of the values are suppressed. This means the data is already well-segmented and can be
                // considered categorical or quasi-categorical.
                var distinctValues =
                    from row in distinctValueQ.ResultRows
                    where !row.DistinctData.IsSuppressed
                    select new
                    {
                        row.DistinctData.Value,
                        row.Count,
                    };

                PublishMetric(new UntypedMetric(name: "distinct_values", metric: distinctValues));
                PublishMetric(new UntypedMetric(name: "suppressed_values", metric: suppressedValueCount));
            }

            await Task.WhenAll(
                LinearBuckets(cancellationToken),
                CyclicalBuckets(cancellationToken));

            // Other metrics?
            // Median
            // Average
        }

        private static object DatetimeMetric<T>(
            long total,
            long suppressed,
            IEnumerable<AircloakValueCount<T>> valueCounts)
        {
            return new
            {
                Total = total,
                Suppressed = suppressed,
                Counts =
                    from valueCount in valueCounts
                    where !valueCount.IsSuppressed
                    orderby valueCount.Value ascending
                    select new
                    {
                        valueCount.Value,
                        valueCount.Count,
                        valueCount.CountNoise,
                    },
            };
        }

        private async Task LinearBuckets(CancellationToken cancellationToken)
        {
            var queryResult = await ResolveQuery(
                new BucketedDatetimes(TableName, ColumnName),
                cancellationToken);

            await Task.Run(
                () => ProcessLinearBuckets(queryResult.ResultRows, cancellationToken),
                cancellationToken);
        }

        private async Task CyclicalBuckets(CancellationToken cancellationToken)
        {
            var queryResult = await ResolveQuery(
                new CyclicalDatetimes(TableName, ColumnName),
                cancellationToken);

            await Task.Run(
                () => ProcessCyclicalBuckets(queryResult.ResultRows, cancellationToken),
                cancellationToken);
        }

        private void ProcessLinearBuckets(
            IEnumerable<BucketedDatetimes.Result> queryResult,
            CancellationToken cancellationToken)
        {
            foreach (var group in GroupByLabel(queryResult))
            {
                cancellationToken.ThrowIfCancellationRequested();

                var label = group.Key;
                var valueCounts = group
                    .Select(row => new AircloakValueCount<DateTime>(row.GroupingValue, row.Count, row.CountNoise));

                var (totalCount, suppressedCount) = valueCounts.CountTotalAndSuppressed();

                var suppressedRatio = (double)suppressedCount / totalCount;

                if (suppressedRatio > SuppressedRatioThreshold)
                {
                    break;
                }

                PublishMetric(new UntypedMetric(name: $"dates_linear.{label}", metric: DatetimeMetric(
                    totalCount, suppressedCount, valueCounts)));
            }
        }

        private void ProcessCyclicalBuckets(
            IEnumerable<CyclicalDatetimes.Result> queryResult,
            CancellationToken cancellationToken)
        {
            var includeRest = false;
            foreach (var group in GroupByLabel(queryResult))
            {
                cancellationToken.ThrowIfCancellationRequested();

                var label = group.Key;

                if (!includeRest)
                {
                    var distinctValueCount = group.Count(row => row.GroupingValue.HasValue);

                    includeRest = (label, distinctValueCount) switch
                    {
                        ("quarter", var count) when count > 4 => true,
                        ("day", var count) when count > 7 => true,
                        (_, var count) when count > 1 => true,
                        _ => false,
                    };

                    continue;
                }

                var valueCounts = group
                    .Select(row => new AircloakValueCount<int>(row.GroupingValue, row.Count, row.CountNoise));

                var (totalCount, suppressedCount) = valueCounts.CountTotalAndSuppressed();

                var suppressedRatio = (double)suppressedCount / totalCount;

                if (suppressedRatio > SuppressedRatioThreshold)
                {
                    break;
                }

                PublishMetric(new UntypedMetric(name: $"dates_cyclical.{label}", metric: DatetimeMetric(
                    totalCount, suppressedCount, valueCounts)));
            }
        }

        private IEnumerable<IGrouping<string, T>> GroupByLabel<T>(IEnumerable<T> queryResult)
            where T : IGroupingSetsAggregate
        {
            return queryResult.GroupBy(row => row.GroupingLabel);
        }

        private class AircloakValueCount<T> : ICountAggregate, INullable, ISuppressible
        {
            private readonly AircloakValue<T> av;

            public AircloakValueCount(AircloakValue<T> av, long count, double? countNoise)
            {
                this.av = av;
                Count = count;
                CountNoise = countNoise;
            }

            public T Value => av.Value;

            public bool IsSuppressed => av.IsSuppressed;

            public bool IsNull => av.IsNull;

            public long Count { get; }

            public double? CountNoise { get; }
        }
    }
}
