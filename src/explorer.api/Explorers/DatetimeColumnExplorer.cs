namespace Explorer
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using Aircloak.JsonApi.ResponseTypes;
    using Explorer.Queries;

    internal class DatetimeColumnExplorer : ExplorerBase
    {
        // TODO: The following should be configuration items (?)
        private const long ValuesPerBucketTarget = 20;

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

            await Task.Run(() => ProcessLinearBuckets(queryResult));
        }

        private async Task CyclicalBuckets()
        {
            var queryResult = await ResolveQuery(
                new CyclicalDatetimes(TableName, ColumnName),
                TimeSpan.FromMinutes(10));

            await Task.Run(() => ProcessCyclicalBuckets(queryResult));
        }

        private void ProcessLinearBuckets(QueryResult<BucketedDatetimes.Result> queryResult)
        {

            PublishMetric(new UntypedMetric(name: "dates_linear", metric: new object { }));
        }

        private void ProcessCyclicalBuckets(QueryResult<CyclicalDatetimes.Result> queryResult)
        {
            // Years are not cyclical
            // Months are y-cyclical where y is the number of distinct years
            // Days are (y * m)-cyclical where m is the number of distinct months
            // etc.
            // If there are fewer than 2 years, don't bother including months
            // If there are fewer than 2 months, don't bother including days
            // "" 2 weeks ... weekdays
            // etc.
            var yearsRows = ExtractValueCounts(queryResult.ResultRows, row => row.Year);
            var (yearsCount, yearsSuppressed) = CountTotalAndSuppressed(yearsRows);

            foreach (var (component, selector) in cyclicalDatetimeComponentSelectors)
            {
                var rows = ExtractValueCounts(queryResult.ResultRows, selector);
                var (totalCount, suppressedCount) = CountTotalAndSuppressed(rows);
                PublishMetric(new UntypedMetric(name: $"dates_cyclical.{component}", metric: new
                {
                    Total = totalCount,
                    Suppressed = suppressedCount,
                    Counts =
                        from tup in rows
                        where !tup.Item1.IsSuppressed
                        let value = tup.Item1.Value
                        orderby value ascending
                        select new
                        {
                            Value = value,
                            Count = tup.Item2,
                            CountNoise = tup.Item3,
                        },
                }));
            }

            var months = ExtractValueCounts(queryResult.ResultRows, row => row.Month);
            var days = ExtractValueCounts(queryResult.ResultRows, row => row.Day);
            var weekdays = ExtractValueCounts(queryResult.ResultRows, row => row.Weekday);
            var hours = ExtractValueCounts(queryResult.ResultRows, row => row.Hour);
            var minutes = ExtractValueCounts(queryResult.ResultRows, row => row.Minute);
            var seconds = ExtractValueCounts(queryResult.ResultRows, row => row.Second);

            PublishMetric(new UntypedMetric(name: "dates_cyclical", metric: months));
        }

        private IEnumerable<(AircloakValue<T>, long, double?)> ExtractValueCounts<T>(
            IEnumerable<CyclicalDatetimes.Result> rows,
            Func<CyclicalDatetimes.Result, AircloakValue<T>> selector) =>
                from row in rows
                let value = selector(row)
                where !value.IsNull
                select (value, count: row.Count, noise: row.CountNoise);

        private (long, long) CountTotalAndSuppressed<T>(IEnumerable<(AircloakValue<T>, long, double?)> valueCounts) =>
            valueCounts.Aggregate(
                (0L, 0L),
                (acc, next) => (
                    acc.Item1 + next.Item2,
                    acc.Item2 + (next.Item1.IsSuppressed ? next.Item2 : 0L)));

        private static readonly Dictionary<string, Func<CyclicalDatetimes.Result, AircloakValue<int>>>
            cyclicalDatetimeComponentSelectors =
                new Dictionary<string, Func<CyclicalDatetimes.Result, AircloakValue<int>>>
                {
                    { "year", _ => _.Year },
                    { "month", _ => _.Month },
                    { "day", _ => _.Day },
                    { "weekday", _ => _.Weekday },
                    { "hour", _ => _.Hour },
                    { "minute", _ => _.Minute },
                    { "second", _ => _.Second },
                };
    }
}
