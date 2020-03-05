namespace Explorer
{
    using System;
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
            PublishMetric(new UntypedMetric(name: "dummy_datehist", metric: new object { }));
        }

        private void ProcessCyclicalBuckets(QueryResult<CyclicalDatetimes.Result> queryResult)
        {
            PublishMetric(new UntypedMetric(name: "dummy_daterepetition", metric: new object { }));
        }
    }
}
