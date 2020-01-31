namespace Explorer.Queries.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Aircloak.JsonApi;
    using Aircloak.JsonApi.ResponseTypes;
    using Xunit;

    public sealed class QueryTests
    {
        private const string AircloakApiKeyEnvVariable = "AIRCLOAK_API_KEY";

        private const string TestDataSource = "gda_banking";

        private static readonly Uri AircloakTestServerUrl = new Uri("https://attack.aircloak.com/api/");

        private readonly JsonApiSession jsonApiSession;

        public QueryTests()
        {
            var apiKey = Environment.GetEnvironmentVariable(AircloakApiKeyEnvVariable) ??
                    throw new System.InvalidOperationException(
                        $"Environment variable {AircloakApiKeyEnvVariable} must be set");

            this.jsonApiSession = JsonApiSessionManager.NewJsonApiSession(AircloakTestServerUrl, apiKey);
        }

        [Fact]
        public async void TestDistinctValueQuery()
        {
            var intResult = await QueryResult<DistinctColumnValues.IntegerResult>(
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

            var realResult = await QueryResult<DistinctColumnValues.RealResult>(
                new DistinctColumnValues(
                    tableName: "loans",
                    columnName: "payments"));

            Assert.All(realResult.ResultRows, row =>
            {
                Assert.True(row.ColumnValue.IsNull || row.ColumnValue.IsSuppressed ||
                    ((ValueColumn<double>)row.ColumnValue).ColumnValue >= 0);
                Assert.True(row.Count.HasValue && row.Count > 0);
            });

            var textResult = await QueryResult<DistinctColumnValues.TextResult>(
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
        public async void TestHistogramQuery()
        {
            var bucketSizes = new List<decimal> { 10_000, 20_000, 50_000 };
            var result = await QueryResult<SingleColumnHistogram.Result>(
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

        private async Task<QueryResult<TResult>> QueryResult<TResult>(IQuerySpec<TResult> query)
            where TResult : IJsonArrayConvertible, new()
        {
            return await jsonApiSession.Query<TResult>(
                TestDataSource,
                query.QueryStatement,
                TimeSpan.FromSeconds(30));
        }
    }
}