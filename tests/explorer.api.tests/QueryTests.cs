namespace Explorer.Api.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Text.Json;
    using System.Threading.Tasks;
    using Aircloak.JsonApi;
    using Aircloak.JsonApi.ResponseTypes;
    using Explorer.Queries;
    using Xunit;

    public sealed class QueryTests : IClassFixture<TestWebAppFactory>
    {
        private const string TestDataSource = "gda_banking";

        private readonly TestWebAppFactory factory;

        public QueryTests(TestWebAppFactory factory)
        {
            this.factory = factory;
        }

        [Fact]
        public async void TestDistinctLoansDuration()
        {
            var intResult = await QueryResult<DistinctColumnValues.IntegerResult>(
                new DistinctColumnValues(
                    tableName: "loans",
                    columnName: "duration"));

            Assert.True(intResult.Query.Completed);
            Assert.True(string.IsNullOrEmpty(intResult.Query.Error), intResult.Query.Error);
            Assert.All(intResult.ResultRows, row =>
            {
                Assert.True(row.ColumnValue.IsNull || row.ColumnValue.IsSuppressed ||
                    ((ValueColumn<long>)row.ColumnValue).ColumnValue >= 0);
                Assert.True(row.Count > 0);
                Assert.True(row.CountNoise.HasValue);
            });
        }

        [Fact]
        public async void TestDistinctLoansPayments()
        {
            var realResult = await QueryResult<DistinctColumnValues.RealResult>(
                new DistinctColumnValues(
                    tableName: "loans",
                    columnName: "payments"));

            Assert.True(realResult.Query.Completed);
            Assert.True(string.IsNullOrEmpty(realResult.Query.Error), realResult.Query.Error);
            Assert.All(realResult.ResultRows, row =>
            {
                Assert.True(row.ColumnValue.IsNull || row.ColumnValue.IsSuppressed ||
                    ((ValueColumn<double>)row.ColumnValue).ColumnValue >= 0);
                Assert.True(row.Count > 0);
            });
        }

        [Fact]
        public async void TestDistinctLoansGender()
        {
            var textResult = await QueryResult<DistinctColumnValues.TextResult>(
                new DistinctColumnValues(
                    tableName: "loans",
                    columnName: "gender"));

            Assert.True(textResult.Query.Completed);
            Assert.True(string.IsNullOrEmpty(textResult.Query.Error), textResult.Query.Error);
            Assert.All(textResult.ResultRows, row =>
            {
                Assert.True(((ValueColumn<string>)row.ColumnValue).ColumnValue == "Male" ||
                            ((ValueColumn<string>)row.ColumnValue).ColumnValue == "Female");
                Assert.True(row.Count > 0);
                Assert.True(row.CountNoise.HasValue);
            });

            Assert.True(textResult.ResultRows.Count() == 2);
        }

        [Fact]
        public async void TestHistogramLoansAmount()
        {
            var bucketSizes = new List<decimal> { 10_000, 20_000, 50_000 };
            var result = await QueryResult<SingleColumnHistogram.Result>(
                new SingleColumnHistogram(
                    "loans",
                    "amount",
                    bucketSizes));

            Assert.True(result.Query.Completed);
            Assert.True(string.IsNullOrEmpty(result.Query.Error), result.Query.Error);
            Assert.All(result.ResultRows, row =>
            {
                Assert.True(row.BucketIndex < bucketSizes.Count);
                Assert.True(row.LowerBound.IsNull ||
                            row.LowerBound.IsSuppressed ||
                            ((ValueColumn<decimal>)row.LowerBound).ColumnValue >= 0);
                Assert.True(row.Count > 0);
                Assert.True(row.CountNoise.HasValue);
            });
        }

        [Fact]
        public async void TestMinMaxExplorer()
        {
            var vcrCassetteInfo = factory.GetVcrCasetteInfo(nameof(QueryTests), nameof(TestMinMaxExplorer));
            using var client = factory.CreateAircloakApiHttpClient(vcrCassetteInfo);
            var jsonApiClient = new JsonApiClient(client);

            var explorer = new MinMaxExplorer(
                jsonApiClient,
                new Models.ExploreParams
                {
                    DataSourceName = "gda_banking",
                    TableName = "loans",
                    ColumnName = "amount",
                });

            await explorer.Explore();

            var final = explorer.LatestResult;
            Assert.True(final.Status == "complete");
            Assert.True((decimal)final.Metrics.Single(m => m.MetricName == "refined_min").MetricValue == 3288M);
            Assert.True((decimal)final.Metrics.Single(m => m.MetricName == "refined_max").MetricValue == 495725M);
        }

        [Fact]
        public async void TestRepeatingRows()
        {
            var queryResult = await QueryResult(new RepeatingRowsQuery());

            Assert.True(queryResult.Query.Completed);
            Assert.True(string.IsNullOrEmpty(queryResult.Query.Error), queryResult.Query.Error);
            Assert.True(queryResult.ResultRows.Count() == 5);
            Assert.All(queryResult.ResultRows, row =>
            {
                Assert.True(row.One == 1);
                Assert.True(row.Two == 2);
                Assert.True(row.Three == 3);
            });
        }

        private async Task<QueryResult<TResult>> QueryResult<TResult>(IQuerySpec<TResult> query, [CallerMemberName] string vcrSessionName = "")
        {
            // WaitDebugger();
            var vcrCassetteInfo = factory.GetVcrCasetteInfo(nameof(QueryTests), vcrSessionName);
            using var client = factory.CreateAircloakApiHttpClient(vcrCassetteInfo);
            var jsonApiClient = new JsonApiClient(client);
            return await jsonApiClient.Query<TResult>(
                TestDataSource,
                query,
                TimeSpan.FromSeconds(30),
                factory.GetApiPollingFrequencty(vcrCassetteInfo));
        }

        private class RepeatingRowsQuery : IQuerySpec<RepeatingRowsQuery.Result>
        {
            public string QueryStatement =>
                @"select 1, 2, 3
                    from loans
                    GROUP BY duration
                    having count_noise(*) > 0";

            public Result FromJsonArray(ref Utf8JsonReader reader)
            {
                reader.Read();
                var one = reader.GetInt32();
                reader.Read();
                var two = reader.GetInt32();
                reader.Read();
                var three = reader.GetInt32();

                return new Result { One = one, Two = two, Three = three };
            }

            public struct Result
            {
                public int One;
                public int Two;
                public int Three;
            }
        }
    }
}
