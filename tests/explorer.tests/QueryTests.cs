namespace Explorer.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.Json;
    using System.Threading.Tasks;

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
            using var queryScope = testFixture.SimpleQueryTestScope(
                "gda_banking",
                vcrFilename: ExplorerTestFixture.GenerateVcrFilename(this));

            var result = await queryScope.QueryRows(
                new DistinctColumnValues(
                    tableName: "loans",
                    columnName: "duration"));

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
            using var queryScope = testFixture.SimpleQueryTestScope(
                "gda_banking",
                vcrFilename: ExplorerTestFixture.GenerateVcrFilename(this));

            var realResult = await queryScope.QueryRows(
                new DistinctColumnValues(
                    tableName: "loans",
                    columnName: "payments"));

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
            using var queryScope = testFixture.SimpleQueryTestScope(
                "gda_banking",
                vcrFilename: ExplorerTestFixture.GenerateVcrFilename(this));

            var textResult = await queryScope.QueryRows(
                new DistinctColumnValues(
                    tableName: "loans",
                    columnName: "gender"));

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
            using var queryScope = testFixture.SimpleQueryTestScope(
                "cov_clear",
                vcrFilename: ExplorerTestFixture.GenerateVcrFilename(this));

            var datetimeResult = await queryScope.QueryRows(
                new DistinctColumnValues(tableName: "survey", columnName: "first_caught"));

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
            using var queryScope = testFixture.SimpleQueryTestScope(
                "gda_banking",
                vcrFilename: ExplorerTestFixture.GenerateVcrFilename(this));

            var bucketSizes = new List<decimal> { 10_000, 20_000, 50_000 };
            var result = await queryScope.QueryRows(
                new SingleColumnHistogram(
                    "loans",
                    "amount",
                    bucketSizes));

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
            using var queryScope = testFixture.SimpleQueryTestScope(
                "gda_taxi",
                vcrFilename: ExplorerTestFixture.GenerateVcrFilename(this));

            var result = await queryScope.QueryRows(
                query: new CyclicalDatetimes(
                    "rides",
                    "pickup_datetime"));

            Assert.All(result, row => Assert.True(row.Count > 0));
        }

        [Fact]
        public async void TestCyclicalDateQueryTaxiBirthdates()
        {
            using var queryScope = testFixture.SimpleQueryTestScope(
                "gda_taxi",
                vcrFilename: ExplorerTestFixture.GenerateVcrFilename(this));

            var result = await queryScope.QueryRows(
                query: new CyclicalDatetimes(
                    "rides",
                    "birthdate",
                    DValueType.Date));

            Assert.All(result, row => Assert.True(row.Count > 0));
        }

        [Fact]
        public async void TestBucketedDatetimeQueryTaxiPickupTimes()
        {
            using var queryScope = testFixture.SimpleQueryTestScope(
                "gda_taxi",
                vcrFilename: ExplorerTestFixture.GenerateVcrFilename(this));

            var result = await queryScope.QueryRows(
                query: new BucketedDatetimes(
                    "rides",
                    "pickup_datetime"));

            Assert.All(result, row => Assert.True(row.Count > 0));
        }

        [Fact]
        public async void TestRepeatingRows()
        {
            using var queryScope = testFixture.SimpleQueryTestScope(
                RepeatingRowsQuery.DataSet,
                vcrFilename: ExplorerTestFixture.GenerateVcrFilename(this));

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
            using var queryScope = testFixture
                .PrepareTestScope()
                .LoadCassette(ExplorerTestFixture.GenerateVcrFilename(this))
                .OverrideVcrOptions(recordingOptions: VcrSharp.RecordingOptions.FailureOnly)
                .WithConnectionParams(testFixture.ApiUri, LongRunningQuery.DataSet);

            var queryTask = Task.Run(() => queryScope.QueryRows(new LongRunningQuery()));

            var ex = await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            {
#pragma warning disable CS4014 // Consider applying the 'await' operator to the result of the call.
                queryScope.CancelQuery(1000);
#pragma warning restore CS4014 // Consider applying the 'await' operator to the result of the call.

                await queryTask;
            });
        }

        private class RepeatingRowsQuery : DQuery<RepeatingRowsQuery.Result>
        {
            public const string DataSet = "gda_banking";

            public string QueryStatement =>
                @"select 1, 2, 3
                    from loans
                    GROUP BY duration
                    having count_noise(*) > 0";

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
    }
}
