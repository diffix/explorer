namespace Explorer.Api.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
<<<<<<< HEAD
    using System.Runtime.CompilerServices;
=======
    using System.Text.Json;
>>>>>>> Take account of 'occurences' in QueryResult Row
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
            Assert.True(string.IsNullOrEmpty(intResult.Query.Error));
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
                new DistinctColumnValues(
                    tableName: "loans",
                    columnName: "payments"));
<<<<<<< HEAD
=======

            Assert.True(realResult.Query.Completed);
            Assert.True(string.IsNullOrEmpty(realResult.Query.Error));
>>>>>>> Fix broken histogram query and associated tests.
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
                new DistinctColumnValues(
                    tableName: "loans",
                    columnName: "gender"));
<<<<<<< HEAD
=======

            Assert.True(textResult.Query.Completed);
            Assert.True(string.IsNullOrEmpty(textResult.Query.Error));
>>>>>>> Fix broken histogram query and associated tests.
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
                new SingleColumnHistogram(
                    "loans",
                    "amount",
                    bucketSizes));

            Assert.True(result.Query.Completed);
            Assert.True(string.IsNullOrEmpty(result.Query.Error));
            Assert.All(result.ResultRows, row =>
            {
                Assert.True(row.BucketIndex.HasValue && row.BucketIndex.Value < bucketSizes.Count);
                Assert.True(row.LowerBound.IsNull ||
                            row.LowerBound.IsSuppressed ||
                            ((ValueColumn<decimal>)row.LowerBound).ColumnValue >= 0);
                Assert.True(row.Count.HasValue && row.Count > 0);
                Assert.True(row.CountNoise.HasValue);
            });
        }

        [Fact]
        public async void TestRepeatingRows()
        {
            var queryResult = await QueryResult<RepeatingRowsQuery.Result>(new RepeatingRowsQuery());


            Assert.True(queryResult.Query.Completed);
            Assert.True(string.IsNullOrEmpty(queryResult.Query.Error));
            Assert.True(queryResult.ResultRows.Count() == 5);
            Assert.All(queryResult.ResultRows, row =>
            {
                Assert.True(row.one == 1);
                Assert.True(row.two == 2);
                Assert.True(row.three == 3);
            });
        }

        private async Task<QueryResult<TResult>> QueryResult<TResult>(IQuerySpec<TResult> query, [CallerMemberName] string vcrSessionName = "")
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

        private class RepeatingRowsQuery : IQuerySpec<RepeatingRowsQuery.Result>
        {
            public string QueryStatement =>
                @"select 1, 2, 3
                    from loans
                    GROUP BY duration
                    having count_noise(*) > 0";

            public struct Result : IJsonArrayConvertible
            {
                public int one;
                public int two;
                public int three;

                public void FromArrayValues(ref Utf8JsonReader reader)
                {
                    reader.Read();
                    one = reader.GetInt32();
                    reader.Read();
                    two = reader.GetInt32();
                    reader.Read();
                    three = reader.GetInt32();
                }
            }
        }
    }
}