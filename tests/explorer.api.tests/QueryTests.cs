namespace Explorer.Api.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
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
                nameof(TestDistinctLoansDuration),
                new DistinctColumnValues(
                    tableName: "loans",
                    columnName: "duration"));

            Assert.All(intResult.ResultRows, row =>
            {
                Assert.True(row.ColumnValue.IsNull || row.ColumnValue.IsSuppressed ||
                    ((ValueColumn<long>)row.ColumnValue).ColumnValue >= 0);
                Assert.True(row.Count.HasValue && row.Count > 0);
                Assert.True(row.CountNoise.HasValue);
            });
        }

        [Fact]
        public async void TestDistinctLoansPayments()
        {
            var realResult = await QueryResult<DistinctColumnValues.RealResult>(
                nameof(TestDistinctLoansPayments),
                new DistinctColumnValues(
                    tableName: "loans",
                    columnName: "payments"));
            Assert.All(realResult.ResultRows, row =>
            {
                Assert.True(row.ColumnValue.IsNull || row.ColumnValue.IsSuppressed ||
                    ((ValueColumn<double>)row.ColumnValue).ColumnValue >= 0);
                Assert.True(row.Count.HasValue && row.Count > 0);
            });
        }

        [Fact]
        public async void TestDistinctLoansGender()
        {
            var textResult = await QueryResult<DistinctColumnValues.TextResult>(
                nameof(TestDistinctLoansGender),
                new DistinctColumnValues(
                    tableName: "loans",
                    columnName: "gender"));
            Assert.All(textResult.ResultRows, row =>
            {
                Assert.True(((ValueColumn<string>)row.ColumnValue).ColumnValue == "Male" ||
                            ((ValueColumn<string>)row.ColumnValue).ColumnValue == "Female");
                Assert.True(row.Count.HasValue && row.Count > 0);
                Assert.True(row.CountNoise.HasValue);
            });

            Assert.True(textResult.ResultRows.Count() == 2);
        }

        [Fact]
        public async void TestHistogramLoansAmount()
        {
            var bucketSizes = new List<decimal> { 10_000, 20_000, 50_000 };
            var result = await QueryResult<SingleColumnHistogram.Result>(
                nameof(TestHistogramLoansAmount),
                new SingleColumnHistogram(
                    "loans",
                    "amount",
                    bucketSizes));

            Assert.All(result.ResultRows, row =>
            {
                Assert.True(bucketSizes.Exists(v => row.BucketSize?.Equals(v) ?? false));
                Assert.True(row.LowerBound.IsNull ||
                            row.LowerBound.IsSuppressed ||
                            ((ValueColumn<decimal>)row.LowerBound).ColumnValue >= 0);
                Assert.True(row.Count.HasValue && row.Count > 0);
                Assert.True(row.CountNoise.HasValue);
            });
        }

        private async Task<QueryResult<TResult>> QueryResult<TResult>(string vcrSessionName, IQuerySpec<TResult> query)
            where TResult : IJsonArrayConvertible, new()
        {
            // WaitDebugger();
            var vcrCassettePath = factory.GetVcrCasettePath(nameof(QueryTests), vcrSessionName);
            var vcrCassetteFile = new System.IO.FileInfo(vcrCassettePath);
            var pollingFrequency = (vcrCassetteFile.Exists && vcrCassetteFile.Length > 0) ? TimeSpan.FromMilliseconds(1) : default(TimeSpan?);
            using var client = factory.CreateAircloakApiHttpClient(vcrCassettePath);
            var jsonApiClient = new JsonApiClient(client);
            return await jsonApiClient.Query<TResult>(
                TestDataSource,
                query.QueryStatement,
                TimeSpan.FromSeconds(30),
                pollingFrequency);
        }
    }
}