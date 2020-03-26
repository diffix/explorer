namespace Explorer.Api.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http;
    using System.Runtime.CompilerServices;
    using System.Text.Json;
    using System.Threading;
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
            var intResult = await QueryResult<DistinctColumnValues.Result>(
                new DistinctColumnValues(
                    tableName: "loans",
                    columnName: "duration"));

            Assert.True(intResult.Query.Completed);
            Assert.True(string.IsNullOrEmpty(intResult.Query.Error), intResult.Query.Error);
            Assert.All(intResult.ResultRows, row =>
            {
                Assert.True(row.DistinctData.IsNull || row.DistinctData.IsSuppressed ||
                    (row.DistinctData.Value.ValueKind == JsonValueKind.Number &&
                    row.DistinctData.Value.GetInt32() >= 0));
                Assert.True(row.Count > 0);
                Assert.True(row.CountNoise.HasValue);
            });
        }

        [Fact]
        public async void TestDistinctLoansPayments()
        {
            var realResult = await QueryResult<DistinctColumnValues.Result>(
                new DistinctColumnValues(
                    tableName: "loans",
                    columnName: "payments"));

            Assert.True(realResult.Query.Completed);
            Assert.True(string.IsNullOrEmpty(realResult.Query.Error), realResult.Query.Error);
            Assert.All(realResult.ResultRows, row =>
            {
                Assert.True(row.DistinctData.IsNull || row.DistinctData.IsSuppressed ||
                    (row.DistinctData.Value.ValueKind == JsonValueKind.Number &&
                    row.DistinctData.Value.GetDouble() >= 0));
                Assert.True(row.Count > 0);
            });
        }

        [Fact]
        public async void TestDistinctLoansGender()
        {
            var textResult = await QueryResult<DistinctColumnValues.Result>(
                new DistinctColumnValues(
                    tableName: "loans",
                    columnName: "gender"));

            Assert.True(textResult.Query.Completed);
            Assert.True(string.IsNullOrEmpty(textResult.Query.Error), textResult.Query.Error);
            Assert.All(textResult.ResultRows, row =>
            {
                Assert.True(row.DistinctData.Value.ValueKind == JsonValueKind.String);
                Assert.True(row.DistinctData.Value.GetString() == "Male" ||
                            row.DistinctData.Value.GetString() == "Female");
                Assert.True(row.Count > 0);
                Assert.True(row.CountNoise.HasValue);
            });

            Assert.True(textResult.ResultRows.Count() == 2);
        }

        [Fact]
        public async void TestDistinctDatetimes()
        {
            var datetimeResult = await QueryResult<DistinctColumnValues.Result>(
                new DistinctColumnValues(tableName: "patients", columnName: "date_of_birth"),
                dataSourceName: "Clinic");

            Assert.True(datetimeResult.Query.Completed);
            Assert.True(string.IsNullOrEmpty(datetimeResult.Query.Error), datetimeResult.Query.Error);
            Assert.True(datetimeResult.ResultRows.Any());
            Assert.All(datetimeResult.ResultRows, row =>
            {
                Assert.True(row.DistinctData.IsNull || row.DistinctData.IsSuppressed ||
                    row.DistinctData.Value.ValueKind == JsonValueKind.String);
                Assert.True(row.Count > 0);
            });
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
                            row.LowerBound.Value >= 0);
                Assert.True(row.Count > 0);
                Assert.True(row.CountNoise.HasValue);
            });
        }

        [Fact]
        public async void TestCyclicalDatetimeQueryTaxiPickupTimes()
        {
            var result = await QueryResult<CyclicalDatetimes.Result>(
                dataSourceName: "gda_taxi",
                query: new CyclicalDatetimes(
                    "rides",
                    "pickup_datetime"));

            Assert.True(result.Query.Completed);
            Assert.Equal("completed", result.Query.QueryState);
            Assert.True(string.IsNullOrEmpty(result.Query.Error), result.Query.Error);
            Assert.All(result.ResultRows, row => Assert.True(row.Count > 0));
        }

        [Fact]
        public async void TestCyclicalDateQueryTaxiBirthdates()
        {
            var result = await QueryResult<CyclicalDatetimes.Result>(
                dataSourceName: "gda_taxi",
                query: new CyclicalDatetimes(
                    "rides",
                    "birthdate",
                    AircloakType.Date));

            Assert.True(result.Query.Completed);
            Assert.Equal("completed", result.Query.QueryState);
            Assert.True(string.IsNullOrEmpty(result.Query.Error), result.Query.Error);
            Assert.All(result.ResultRows, row => Assert.True(row.Count > 0));
        }

        [Fact]
        public async void TestBucketedDatetimeQueryTaxiPickupTimes()
        {
            var result = await QueryResult<BucketedDatetimes.Result>(
                dataSourceName: "gda_taxi",
                query: new BucketedDatetimes(
                    "rides",
                    "pickup_datetime"));

            Assert.True(result.Query.Completed);
            Assert.Equal("completed", result.Query.QueryState);
            Assert.True(string.IsNullOrEmpty(result.Query.Error), result.Query.Error);
            Assert.All(result.ResultRows, row => Assert.True(row.Count > 0));
        }

        [Fact]
        public async void TestMinMaxExplorer()
        {
            var metrics = await GetExplorerMetrics("gda_banking", queryResolver =>
                new MinMaxExplorer(queryResolver, "loans", "amount"));

            const decimal expectedMin = 3288M;
            const decimal expectedMax = 495725M;
            var actualMin = (decimal)metrics.Single(m => m.Name == "refined_min").Metric;
            Assert.True(actualMin == expectedMin, $"Expected {expectedMin}, got {actualMin}");
            var actualMax = (decimal)metrics.Single(m => m.Name == "refined_max").Metric;
            Assert.True(actualMax == expectedMax, $"Expected {expectedMax}, got {actualMax}");
        }

        [Fact]
        public async void TestCategoricalBoolExplorer()
        {
            var metrics = await GetExplorerMetrics("GiveMeSomeCredit", queryResolver =>
                new CategoricalColumnExplorer(queryResolver, "loans", "SeriousDlqin2yrs"));

            var expectedValues = new List<object>
            {
                new { Value = false, Count = 139_974L },
                new { Value = true, Count = 10_028L },
            };

            CheckDistinctCategories(metrics, expectedValues, el => el.GetBoolean());
        }

        [Fact]
        public async void TestCategoricalTextExplorer()
        {
            var metrics = await GetExplorerMetrics("gda_banking", queryResolver =>
                new CategoricalColumnExplorer(queryResolver, "loans", "status"));

            var expectedValues = new List<object>
            {
                new { Value = "C", Count = 493L },
                new { Value = "A", Count = 260L },
                new { Value = "D", Count = 42L },
                new { Value = "B", Count = 32L },
            };

            CheckDistinctCategories(metrics, expectedValues, el => el.GetString());
        }

        [Fact]
        public async void TestDateTimeColumnExplorer()
        {
            var metrics = await GetExplorerMetrics("gda_taxi", queryResolver =>
                new DatetimeColumnExplorer(queryResolver, "rides", "pickup_datetime"));

            Assert.Single(metrics, m => m.Name == "dates_linear.minute");
            Assert.Single(metrics, m => m.Name == "dates_linear.hour");
            Assert.Single(metrics, m => m.Name == "dates_cyclical.second");
            Assert.Single(metrics, m => m.Name == "dates_cyclical.minute");
        }

        [Fact]
        public async void TestDateColumnExplorer()
        {
            var metrics = await GetExplorerMetrics("gda_taxi", queryResolver =>
                new DatetimeColumnExplorer(queryResolver, "rides", "birthdate", AircloakType.Date));

            Assert.Single(metrics, m => m.Name == "dates_linear.year");
            Assert.Single(metrics, m => m.Name == "dates_linear.month");
            Assert.Single(metrics, m => m.Name == "dates_cyclical.day");
            Assert.Single(metrics, m => m.Name == "dates_cyclical.weekday");
            Assert.Single(metrics, m => m.Name == "dates_cyclical.month");
            Assert.Single(metrics, m => m.Name == "dates_cyclical.quarter");
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

        [Fact]
        public async void TestCancelQuery()
        {
            var testConfig = factory.GetTestConfig(nameof(QueryTests), "TestCancelQuery");
            var jsonApiClient = factory.CreateJsonApiClient(testConfig.VcrCassettePath);
            var query = new LongRunningQuery();

            var queryInfo = await jsonApiClient.SubmitQuery(
                LongRunningQuery.DataSet,
                query.QueryStatement,
                CancellationToken.None);

            using var cts = new CancellationTokenSource();
            cts.Cancel();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                jsonApiClient.PollQueryUntilComplete(queryInfo.QueryId, query, testConfig.PollFrequency, cts.Token));

            var ex = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                jsonApiClient.PollQueryUntilComplete(queryInfo.QueryId, query, testConfig.PollFrequency, CancellationToken.None));

            Assert.StartsWith("Aircloak API query canceled", ex.Message, StringComparison.InvariantCultureIgnoreCase);
        }

        private void CheckDistinctCategories(
            IEnumerable<IExploreMetric> distinctMetrics,
            IEnumerable<dynamic> expectedValues,
            Func<JsonElement, dynamic> parseElement)
        {
            var distinctValues = (IEnumerable<dynamic>)distinctMetrics.Single(m => m.Name == "top_distinct_values").Metric;

            foreach (var (actual, expected) in distinctValues.Zip(expectedValues))
            {
                Assert.True(parseElement(actual.Value) == expected.Value, $"Expected {expected}, got {actual}.");
                Assert.True(actual.Count == expected.Count, $"Expected {expected}, got {actual}.");
            }

            var expectedTotal = expectedValues.Sum(v => (long)v.Count);
            var actualTotal = (long)distinctMetrics
                .Single(m => m.Name == "total_count")
                .Metric;
            Assert.True(expectedTotal == actualTotal, $"Expected total of {expectedTotal}, got {actualTotal}");

            const long expectedSuppressed = 0L;
            var actualSuppressed = (long)distinctMetrics
                .Single(m => m.Name == "suppressed_values")
                .Metric;
            Assert.True(
                actualSuppressed == expectedSuppressed,
                $"Expected total of {expectedSuppressed}, got {actualSuppressed}");
        }

        private async Task<QueryResult<TResult>> QueryResult<TResult>(
            IQuerySpec<TResult> query,
            string dataSourceName = TestDataSource,
            [CallerMemberName] string vcrSessionName = "")
        {
            return await factory.QueryResult(query, dataSourceName, nameof(QueryTests), vcrSessionName);
        }

        private async Task<IEnumerable<IExploreMetric>> GetExplorerMetrics(
            string dataSourceName,
            Func<IQueryResolver, ExplorerBase> explorerFactory,
            [CallerMemberName] string vcrSessionName = "")
        {
            return await factory.GetExplorerMetrics(dataSourceName, explorerFactory, nameof(QueryTests), vcrSessionName);
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

        private class LongRunningQuery : IQuerySpec<LongRunningQuery.Result>
        {
            public const string DataSet = "gda_taxi";

            public string QueryStatement =>
                @"select
                    date_trunc('year', pickup_datetime),
                    date_trunc('quarter', pickup_datetime),
                    date_trunc('month', pickup_datetime),
                    date_trunc('day', pickup_datetime),
                    date_trunc('hour', pickup_datetime),
                    date_trunc('minute', pickup_datetime),
                    date_trunc('second', pickup_datetime),
                    grouping_id(
                        date_trunc('year', pickup_datetime),
                        date_trunc('quarter', pickup_datetime),
                        date_trunc('month', pickup_datetime),
                        date_trunc('day', pickup_datetime),
                        date_trunc('hour', pickup_datetime),
                        date_trunc('minute', pickup_datetime),
                        date_trunc('second', pickup_datetime)
                    ),
                    count(*),
                    count_noise(*)
                    from rides
                    group by grouping sets (1, 2, 3, 4, 5, 6, 7)";

            public Result FromJsonArray(ref Utf8JsonReader reader)
            {
                while (reader.TokenType != JsonTokenType.EndArray)
                {
                    reader.Read();
                }

                return default;
            }

            public struct Result
            {
            }
        }
    }
}
