namespace Explorer.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.Json;
    using System.Threading.Tasks;

    using Aircloak.JsonApi.Exceptions;
    using Diffix;
    using Explorer.Queries;
    using Xunit;

    public class QueryTests : IClassFixture<ExplorerTestFixture>
    {
        private readonly ExplorerTestFixture testFixture;

        public QueryTests(ExplorerTestFixture testFixture)
        {
            this.testFixture = testFixture;
        }

        [Fact]
        public async void TestDistinctLoansDuration()
        {
            using var queryScope = await testFixture.CreateTestScope("gda_banking", "loans", "duration", this);

            var result = await queryScope.QueryRows(new DistinctColumnValues());

            Assert.All(result, row =>
            {
                Assert.True(row.IsNull || row.IsSuppressed ||
                    (row.Value.ValueKind == JsonValueKind.Number &&
                    row.Value.GetInt32() >= 0));
                Assert.True(row.Count > 0);
                Assert.True(row.CountNoise.HasValue);
            });
        }

        [Fact]
        public async void TestDistinctLoansPayments()
        {
            using var queryScope = await testFixture.CreateTestScope("gda_banking", "loans", "payments", this);

            var realResult = await queryScope.QueryRows(new DistinctColumnValues());

            Assert.All(realResult, row =>
            {
                Assert.True(row.IsNull || row.IsSuppressed ||
                    (row.Value.ValueKind == JsonValueKind.Number &&
                    row.Value.GetDouble() >= 0));
                Assert.True(row.Count > 0);
            });
        }

        [Fact]
        public async void TestDistinctLoansGender()
        {
            using var queryScope = await testFixture.CreateTestScope("gda_banking", "loans", "gender", this);

            var textResult = await queryScope.QueryRows(new DistinctColumnValues());

            Assert.All(textResult, row =>
            {
                Assert.True(row.Value.ValueKind == JsonValueKind.String);
                Assert.True(row.Value.GetString() == "Male" ||
                            row.Value.GetString() == "Female");
                Assert.True(row.Count > 0);
                Assert.True(row.CountNoise.HasValue);
            });

            Assert.True(textResult.Count() == 2);
        }

        [Fact]
        public async void TestDistinctDatetimes()
        {
            using var queryScope = await testFixture.CreateTestScope("cov_clear", "survey", "first_caught", this);

            var datetimeResult = await queryScope.QueryRows(new DistinctColumnValues());

            Assert.True(datetimeResult.Any());
            Assert.All(datetimeResult, row =>
            {
                Assert.True(row.IsNull || row.IsSuppressed ||
                    row.Value.ValueKind == JsonValueKind.String);
                Assert.True(row.Count > 0);
            });
        }

        [Fact]
        public async void TestHistogramLoansAmount()
        {
            using var queryScope = await testFixture.CreateTestScope("gda_banking", "loans", "amount", this);

            var bucketSizes = new List<decimal> { 10_000, 20_000, 50_000 };
            var result = await queryScope.QueryRows(new SingleColumnHistogram(bucketSizes));

            Assert.All(result, row =>
            {
                Assert.True(row.GroupingIndex < bucketSizes.Count);
                Assert.True(row.IsNull ||
                            row.IsSuppressed ||
                            row.LowerBound >= 0);
                Assert.True(row.Count > 0);
            });
        }

        [Fact]
        public async void TestCyclicalDatetimeQueryTaxiPickupTimes()
        {
            using var queryScope = await testFixture.CreateTestScope("gda_taxi", "rides", "pickup_datetime", this);

            var result = await queryScope.QueryRows(new CyclicalDatetimes());

            Assert.All(result, row => Assert.True(row.Count > 0));
        }

        [Fact]
        public async void TestCyclicalDateQueryTaxiBirthdates()
        {
            using var queryScope = await testFixture.CreateTestScope("gda_taxi", "rides", "birthdate", this);

            var result = await queryScope.QueryRows(new CyclicalDatetimes(DValueType.Date));

            Assert.All(result, row => Assert.True(row.Count > 0));
        }

        [Fact]
        public async void TestBucketedDatetimeQueryTaxiPickupTimes()
        {
            using var queryScope = await testFixture.CreateTestScope("gda_taxi", "rides", "pickup_datetime", this);

            var result = await queryScope.QueryRows(new BucketedDatetimes());

            Assert.All(result, row => Assert.True(row.Count > 0));
        }

        [Fact]
        public async void TestRepeatingRows()
        {
            using var queryScope = await testFixture.CreateTestScope("gda_banking", "loans", "duration", this);

            var queryResult = await queryScope.QueryRows(new RepeatingRowsQuery());

            Assert.True(queryResult.Count() == 5);
            Assert.All(queryResult, row =>
            {
                Assert.True(row.One == 1);
                Assert.True(row.Two == 2);
                Assert.True(row.Three == 3);
            });
        }

        [Fact]
        public async void TestCancelQuery()
        {
            using var queryScope = await testFixture.CreateTestScope("gda_taxi", "rides", "pickup_datetime", this, VcrSharp.RecordingOptions.FailureOnly);

            var queryTask = Task.Run(() => queryScope.QueryRows(new LongRunningQuery()));

            var ex = await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            {
#pragma warning disable CS4014 // Consider applying the 'await' operator to the result of the call.
                queryScope.CancelQueryAfter(1000);
#pragma warning restore CS4014 // Consider applying the 'await' operator to the result of the call.

                await queryTask;
            });
        }

        [Fact]
        public async void TestBadQueryThrowsException()
        {
            using var queryScope = await testFixture.CreateTestScope("gda_banking", "loans", "duration", this);

            await Assert.ThrowsAnyAsync<ApiException>(async () => await queryScope.QueryRows(new BadQuery()));
        }

        private class RepeatingRowsQuery : DQuery<RepeatingRowsQuery.Result>
        {
            public string GetQueryStatement(string table, string column)
            {
                return $@"select 1, 2, 3
                    from {table}
                    GROUP BY {column}
                    having count_noise(*) > 0";
            }

            public Result ParseRow(ref Utf8JsonReader reader)
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

        private class LongRunningQuery : DQuery<LongRunningQuery.Result>
        {
            public string GetQueryStatement(string table, string column)
            {
                return $@"select
                    date_trunc('year', {column}),
                    date_trunc('quarter', {column}),
                    date_trunc('month', {column}),
                    date_trunc('day', {column}),
                    date_trunc('hour', {column}),
                    date_trunc('minute', {column}),
                    date_trunc('second', {column}),
                    grouping_id(
                        date_trunc('year', {column}),
                        date_trunc('quarter', {column}),
                        date_trunc('month', {column}),
                        date_trunc('day', {column}),
                        date_trunc('hour', {column}),
                        date_trunc('minute', {column}),
                        date_trunc('second', {column})
                    ),
                    count(*),
                    count_noise(*)
                    from {table}
                    group by grouping sets (1, 2, 3, 4, 5, 6, 7)";
            }

            public Result ParseRow(ref Utf8JsonReader reader)
            {
                for (var i = 0; i < 10; i++)
                {
                    reader.Read();
                }

                return default;
            }

            public struct Result
            {
            }
        }

        private class BadQuery : DQuery<BadQuery.Result>
        {
            public string GetQueryStatement(string table, string column)
            {
                _ = table;
                _ = column;
                return "this is not a query";
            }

            public Result ParseRow(ref Utf8JsonReader reader)
            {
                return default;
            }

            public struct Result
            {
            }
        }
    }
}
