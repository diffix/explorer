﻿namespace Explorer.Tests
{
    using System.Linq;

    using Diffix;
    using Explorer.Common;
    using Explorer.Components;
    using Xunit;

    public class NumericDistributionTests : IClassFixture<ExplorerTestFixture>
    {
        private readonly ExplorerTestFixture testFixture;

        public NumericDistributionTests(ExplorerTestFixture testFixture)
        {
            this.testFixture = testFixture;
        }

        [Fact]
        public async void TestNumericSampleGenerator()
        {
            using var scope = testFixture.SimpleComponentTestScope(
                "gda_banking",
                "loans",
                "duration",
                new ColumnInfo(DValueType.Integer, ColumnInfo.ColumnType.Regular),
                ExplorerTestFixture.GenerateVcrFilename(this));

            await scope.MetricsTest<NumericSampleGenerator>(result =>
            {
                // TODO: Check metrics aga
                Assert.True(result.Any());
            });
        }

        [Fact]
        public async void TestDistributionAnalysis()
        {
            using var scope = testFixture.SimpleComponentTestScope(
                "GiveMeSomeCredit",
                "loans",
                "age",
                new ColumnInfo(DValueType.Integer, ColumnInfo.ColumnType.Regular),
                ExplorerTestFixture.GenerateVcrFilename(this));

            await scope.MetricsTest<DistributionAnalysisComponent>(result =>
                Assert.True(result.Any()));
        }

        [Fact]
        public async void TestDescriptiveStatsPublisher()
        {
            using var scope = testFixture.SimpleComponentTestScope(
                "GiveMeSomeCredit",
                "loans",
                "age",
                new ColumnInfo(DValueType.Integer, ColumnInfo.ColumnType.Regular),
                ExplorerTestFixture.GenerateVcrFilename(this));

            await scope.MetricsTest<DescriptiveStatsPublisher>(result =>
                Assert.True(result.Any()));
        }
    }
}
