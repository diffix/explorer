namespace Explorer.Queries.Tests
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using Aircloak.JsonApi;
    using Aircloak.JsonApi.ResponseTypes;
    using Explorer.Api.Tests;
    using Xunit;

    public sealed class QueryTests
    {
        private const string TestDataSource = "gda_banking";

        private readonly JsonApiSession jsonApiSession = TestUtils.CreateAircloakApiSession();

        [Fact]
        public async void TestDistinctValueQuery()
        {
            var intResult = await QueryResult<DistinctColumnValues.IntegerResult>(
                new DistinctColumnValues(
                    tableName: "loans",
                    columnName: "duration"));

            Assert.All(intResult.ResultRows, row =>
            {
                Assert.True(row.ColumnValue.HasValue);
                Assert.True(row.Count.HasValue && row.Count > 0);
                Assert.True(row.CountNoise.HasValue);
            });

            var realResult = await QueryResult<DistinctColumnValues.RealResult>(
                new DistinctColumnValues(
                    tableName: "loans",
                    columnName: "payments"));

            Assert.All(realResult.ResultRows, row =>
            {
                Assert.True(row.Count.HasValue && row.Count > 0);
            });

            var textResult = await QueryResult<DistinctColumnValues.TextResult>(
                new DistinctColumnValues(
                    tableName: "loans",
                    columnName: "gender"));

            Assert.All(textResult.ResultRows, row =>
            {
                Assert.True(row.ColumnValue == "Male" || row.ColumnValue == "Female");
                Assert.True(row.Count.HasValue && row.Count > 0);
                Assert.True(row.CountNoise.HasValue);
            });

            Assert.True(textResult.ResultRows.Count() == 2);
        }

        private async Task<QueryResult<TResult>> QueryResult<TResult>(DistinctColumnValues query)
            where TResult : IJsonArrayConvertible, new()
        {
            return await jsonApiSession.Query<TResult>(
                TestDataSource,
                query.QueryStatement,
                TimeSpan.FromSeconds(30));
        }
    }
}