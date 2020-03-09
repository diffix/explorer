namespace Explorer
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using Aircloak.JsonApi.ResponseTypes;

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

        public override async Task Explore()
        {
            var stats = (await ResolveQuery<NumericColumnStats.Result<DateTime>>(
                new NumericColumnStats(TableName, ColumnName),
                timeout: TimeSpan.FromMinutes(2)))
                .ResultRows
                .Single();

            PublishMetric(new UntypedMetric(name: "naive_min", metric: stats.Min));
            PublishMetric(new UntypedMetric(name: "naive_max", metric: stats.Max));

            var distinctValueQ = await ResolveQuery<DistinctColumnValues.Result<DateTime>>(
                new DistinctColumnValues(TableName, ColumnName),
                timeout: TimeSpan.FromMinutes(2));

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

            // var bucketsToSample = DiffixUtilities.EstimateBucketResolutions(
            //     stats.Count, stats.Min, stats.Max, ValuesPerBucketTarget);

            await Task.WhenAll(LinearBuckets(), CyclicalBuckets());

            // Other metrics?
            // Median
            // Average
        }

        private async Task LinearBuckets()
        {
            var queryResult = await ResolveQuery(
                new BucketedDatetimes(TableName, ColumnName),
                TimeSpan.FromMinutes(10));

            await Task.Run(() => ProcessLinearBuckets(queryResult.ResultRows));
        }

        private async Task CyclicalBuckets()
        {
            var queryResult = await ResolveQuery(
                new CyclicalDatetimes(TableName, ColumnName),
                TimeSpan.FromMinutes(10));

            await Task.Run(() => ProcessCyclicalBuckets(queryResult.ResultRows));
        }

        private void ProcessLinearBuckets(IEnumerable<BucketedDatetimes.Result> queryResult)
        {


            PublishMetric(new UntypedMetric(name: "dates_linear", metric: new object { }));
        }

        private void ProcessCyclicalBuckets(IEnumerable<CyclicalDatetimes.Result> queryResult)
        {
            var includeRest = false;
            foreach (var (component, selector) in new (string, Func<CyclicalDatetimes.Result, AircloakValue<int>>)[]
            {
                ("year", row => row.Year),
                ("quarter", row => row.Year),
                ("month", row => row.Month),
                ("day", row => row.Day),
                ("weekday", row => row.Weekday),
                ("hour", row => row.Hour),
                ("minute", row => row.Minute),
                ("second", row => row.Second),
            })
            {
                if (includeRest == false)
                {
                    var distinctValueCount = queryResult.Count(row => !selector(row).IsSuppressed);
                    includeRest = (component, distinctValueCount) switch
                    {
                        ("quarter", var count) when count > 4 => true,
                        ("day", var count) when count > 7 => true,
                        (_, var count) when count > 1 => true,
                        _ => false,
                    };

                    continue;
                }
                var selected = queryResult
                    .Where(row => !selector(row).IsNull)
                    .Select(row => new AircloakValueCount<int>(selector(row), row.Count, row.CountNoise));

                var (totalCount, suppressedCount) = CountTotalAndSuppressed(selected);
                PublishMetric(new UntypedMetric(name: $"dates_cyclical.{component}", metric: new
                {
                    Total = totalCount,
                    Suppressed = suppressedCount,
                    Counts =
                        from valueCount in selected
                        where !valueCount.IsSuppressed
                        orderby valueCount.Value ascending
                        select new
                        {
                            valueCount.Value,
                            valueCount.Count,
                            valueCount.CountNoise,
                        },
                }));
            }
        }

        private (long, long) CountTotalAndSuppressed<T>(IEnumerable<T> valueCounts)
        where T : ICountAggregate, INullable, ISuppressible
        => valueCounts.Aggregate(
                (0L, 0L),
                (acc, next) => (
                    acc.Item1 + next.Count,
                    acc.Item2 + (next.IsSuppressed ? next.Count : 0L)));

        private class ValueCount : ICountAggregate, INullable, ISuppressible
        {
            public bool IsSuppressed { get; set; }

            public bool IsNull { get; set; }

            public long Count { get; set; }

            public double? CountNoise { get; set; }
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

            public long Count { get; set; }

            public double? CountNoise { get; set; }
        }
    }
}
