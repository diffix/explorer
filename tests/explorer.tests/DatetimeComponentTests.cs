namespace Explorer.Tests
{
    using System.Linq;

    using Diffix;
    using Explorer.Common;
    using Explorer.Components;
    using Xunit;

    public class DatetimeComponentTests : IClassFixture<ExplorerTestFixture>
    {
        private readonly ExplorerTestFixture testFixture;

        public DatetimeComponentTests(ExplorerTestFixture testFixture)
        {
            this.testFixture = testFixture;
        }

        [Fact]
        public async void TestLinearDateTimeComponentWithDateTimeColumn()
        {
            using var scope = testFixture.SimpleComponentTestScope(
                "gda_taxi",
                "rides",
                "pickup_datetime",
                new ColumnInfo(DValueType.Datetime, ColumnInfo.ColumnType.Regular),
                ExplorerTestFixture.GenerateVcrFilename(this));

            await scope.ResultTest<LinearTimeBuckets, LinearTimeBuckets.Result>(result =>
            {
                Assert.Single(result.Rows, r => r.Key == "minute");
                Assert.Single(result.Rows, r => r.Key == "hour");
            });
        }

        [Fact]
        public async void TestLinearDateTimeComponentWithDateColumn()
        {
            using var scope = testFixture.SimpleComponentTestScope(
                "gda_taxi",
                "rides",
                "birthdate",
                new ColumnInfo(DValueType.Date, ColumnInfo.ColumnType.Isolating),
                ExplorerTestFixture.GenerateVcrFilename(this));

            await scope.ResultTest<LinearTimeBuckets, LinearTimeBuckets.Result>(result =>
            {
                Assert.Single(result.Rows, r => r.Key == "year");
                Assert.Single(result.Rows, r => r.Key == "quarter");
            });
        }

        [Fact]
        public async void TestCyclicalDateTimeComponentWithDateTimeColumn()
        {
            using var scope = testFixture.SimpleComponentTestScope(
                "gda_taxi",
                "rides",
                "pickup_datetime",
                new ColumnInfo(DValueType.Datetime, ColumnInfo.ColumnType.Regular),
                ExplorerTestFixture.GenerateVcrFilename(this));

            await scope.ResultTest<CyclicalTimeBuckets, CyclicalTimeBuckets.Result>(result =>
            {
                Assert.True(result.Rows.Count() == 2);
                Assert.Single(result.Rows, r => r.Key == "second");
                Assert.Single(result.Rows, r => r.Key == "minute");
            });
        }

        [Fact]
        public async void TestCyclicalDateTimeComponentWithDateColumn()
        {
            using var scope = testFixture.SimpleComponentTestScope(
                "gda_taxi",
                "rides",
                "birthdate",
                new ColumnInfo(DValueType.Date, ColumnInfo.ColumnType.Isolating),
                ExplorerTestFixture.GenerateVcrFilename(this));

            await scope.ResultTest<CyclicalTimeBuckets, CyclicalTimeBuckets.Result>(result =>
            {
                Assert.True(result.Rows.Count() == 4);
                Assert.Single(result.Rows, r => r.Key == "day");
                Assert.Single(result.Rows, r => r.Key == "weekday");
                Assert.Single(result.Rows, r => r.Key == "month");
                Assert.Single(result.Rows, r => r.Key == "quarter");
            });
        }
    }
}