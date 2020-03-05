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
            var intResult = await QueryResult<DistinctColumnValues.Result<long>>(
                new DistinctColumnValues(
                    tableName: "loans",
                    columnName: "duration"));

            Assert.True(intResult.Query.Completed);
            Assert.True(string.IsNullOrEmpty(intResult.Query.Error), intResult.Query.Error);
            Assert.All(intResult.ResultRows, row =>
            {
                Assert.True(row.DistinctData.IsNull || row.DistinctData.IsSuppressed ||
                    row.DistinctData.Value >= 0);
                Assert.True(row.Count > 0);
                Assert.True(row.CountNoise.HasValue);
            });
        }

        [Fact]
        public async void TestDistinctLoansPayments()
        {
            var realResult = await QueryResult<DistinctColumnValues.Result<double>>(
                new DistinctColumnValues(
                    tableName: "loans",
                    columnName: "payments"));

            Assert.True(realResult.Query.Completed);
            Assert.True(string.IsNullOrEmpty(realResult.Query.Error), realResult.Query.Error);
            Assert.All(realResult.ResultRows, row =>
            {
                Assert.True(row.DistinctData.IsNull || row.DistinctData.IsSuppressed ||
                    row.DistinctData.Value >= 0);
                Assert.True(row.Count > 0);
            });
        }

        [Fact]
        public async void TestDistinctLoansGender()
        {
            var textResult = await QueryResult<DistinctColumnValues.Result<string>>(
                new DistinctColumnValues(
                    tableName: "loans",
                    columnName: "gender"));

            Assert.True(textResult.Query.Completed);
            Assert.True(string.IsNullOrEmpty(textResult.Query.Error), textResult.Query.Error);
            Assert.All(textResult.ResultRows, row =>
            {
                Assert.True(row.DistinctData.Value == "Male" ||
                            row.DistinctData.Value == "Female");
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
                    "pickup_datetime"
                )
            );

            Assert.True(result.Query.Completed);
            Assert.True(string.IsNullOrEmpty(result.Query.Error), result.Query.Error);
            Assert.All(result.ResultRows, row =>
            {
                Assert.True(row.Count > 0);
                Assert.False(
                    row.Year.IsNull &&
                    row.Quarter.IsNull &&
                    row.Month.IsNull &&
                    row.Day.IsNull &&
                    row.Weekday.IsNull &&
                    row.Hour.IsNull &&
                    row.Minute.IsNull &&
                    row.Second.IsNull);
            });
        }

        [Fact]
        public async void TestBucketedDatetimeQueryTaxiPickupTimes()
        {
            var result = await QueryResult<BucketedDatetimes.Result>(
                dataSourceName: "gda_taxi",
                query: new BucketedDatetimes(
                    "rides",
                    "pickup_datetime"
                )
            );

            Assert.True(result.Query.Completed);
            Assert.True(string.IsNullOrEmpty(result.Query.Error), result.Query.Error);
            Assert.All(result.ResultRows, row =>
            {
                Assert.True(row.Count > 0);
                Assert.False(
                    row.Year.IsNull &&
                    row.Quarter.IsNull &&
                    row.Month.IsNull &&
                    row.Day.IsNull &&
                    row.Hour.IsNull &&
                    row.Minute.IsNull &&
                    row.Second.IsNull);
            });
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
                new BoolColumnExplorer(queryResolver, "loans", "SeriousDlqin2yrs"));

            var expectedValues = new List<object>
            {
                new { Value = false, Count = 139_974L },
                new { Value = true, Count = 10_028L },
            };

            CheckDistinctCategories(metrics, expectedValues);
        }

        [Fact]
        public async void TestCategoricalTextExplorer()
        {
            var metrics = await GetExplorerMetrics("gda_banking", queryResolver =>
                new TextColumnExplorer(queryResolver, "loans", "status"));

            var expectedValues = new List<object>
            {
                new { Value = "C", Count = 493L },
                new { Value = "A", Count = 260L },
                new { Value = "D", Count = 42L },
                new { Value = "B", Count = 32L },
            };

            CheckDistinctCategories(metrics, expectedValues);
        }

        [Fact]
        public async void TestDateTimeColumnExplorer()
        {
            var metrics = await GetExplorerMetrics("gda_taxi", queryResolver =>
                new DatetimeColumnExplorer(queryResolver, "rides", "pickup_datetime"));

            Assert.Single(metrics, m => m.Name == "dates_linear");
            Assert.Single(metrics, m => m.Name == "dates_cyclical");
            Assert.Single(metrics, m => m.Name == "dates_cyclical.second");
            Assert.Single(metrics, m => m.Name == "dates_cyclical.minute");
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
            var vcrCassetteInfo = factory.GetVcrCasetteInfo(nameof(QueryTests), nameof(TestCancelQuery));
            using var client = factory.CreateAircloakApiHttpClient(vcrCassetteInfo);
            var authProvider = factory.EnvironmentVariableAuthProvider();
            var pollFrequency = TimeSpan.FromMilliseconds(10);
            var jsonApiClient = new JsonApiClient(client, authProvider);
            var bucketSizes = new List<decimal> { 10_000, 20_000, 50_000 };
            var query = new SingleColumnHistogram("loans", "amount", bucketSizes);

            var queryInfo = await jsonApiClient.SubmitQuery("gda_banking", query.QueryStatement, CancellationToken.None);

            using var cts = new CancellationTokenSource();
            cts.Cancel();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                jsonApiClient.PollQueryUntilComplete(queryInfo.QueryId, query, pollFrequency, cts.Token));

            try
            {
                // check that Aircloak query was canceled or completed
                await jsonApiClient.PollQueryUntilComplete(queryInfo.QueryId, query, pollFrequency, CancellationToken.None);
            }
            catch (OperationCanceledException)
            {
                // we ignore this because it's a valid result: it might happen because the query was cancelled;
                // but sometimes the API query will complete, so the exception is not always thrown.
                // TODO: this should be modified to always check for exceptions if we find some query that can be canceled realliably on the Aircloak system.
            }
        }

        private void CheckDistinctCategories(
            IEnumerable<IExploreMetric> distinctMetrics,
            IEnumerable<dynamic> expectedValues)
        {
            var distinctValues =
                (IEnumerable<dynamic>)distinctMetrics
                .Single(m => m.Name == "top_distinct_values")
                .Metric;

            Assert.All<(dynamic, dynamic)>(distinctValues.Zip(expectedValues), tuple =>
            {
                var actual = tuple.Item1;
                var expected = tuple.Item2;
                Assert.True(actual.Value == expected.Value, $"Expected {expected}, got {actual}.");
                Assert.True(actual.Count == expected.Count, $"Expected {expected}, got {actual}.");
            });

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
            var vcrCassetteInfo = factory.GetVcrCasetteInfo(nameof(QueryTests), vcrSessionName);
            using var client = factory.CreateAircloakApiHttpClient(vcrCassetteInfo);
            var authProvider = factory.EnvironmentVariableAuthProvider();
            var jsonApiClient = new JsonApiClient(client, authProvider);

            return await jsonApiClient.Query(
                dataSourceName,
                query,
                factory.GetApiPollingFrequency(vcrCassetteInfo),
                CancellationToken.None);
        }

        private async Task<IEnumerable<IExploreMetric>> GetExplorerMetrics(
            string dataSourceName,
            Func<IQueryResolver, ExplorerBase> explorerFactory,
            [CallerMemberName] string vcrSessionName = "")
        {
            var vcrCassetteInfo = factory.GetVcrCasetteInfo(nameof(QueryTests), vcrSessionName);
            using var client = factory.CreateAircloakApiHttpClient(vcrCassetteInfo);
            var authProvider = factory.EnvironmentVariableAuthProvider();
            var pollFrequency = factory.GetApiPollingFrequency(vcrCassetteInfo);
            var jsonApiClient = new JsonApiClient(client, authProvider);

            var queryResolver = new AircloakQueryResolver(jsonApiClient, dataSourceName, pollFrequency);

            var explorer = new Exploration(new[] { explorerFactory(queryResolver), });

            await explorer.Completion;

            return explorer.ExploreMetrics;
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
