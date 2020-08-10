namespace Explorer.Tests
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
            using var scope = await testFixture.CreateTestScope("gda_banking", "loans", "amount", this);

            await scope.MetricsTest<NumericSampleGenerator>(result =>
            {
                // TODO: Check metrics against schema definition
                Assert.True(result.Any());
            });
        }

        [Fact]
        public async void TestDistributionAnalysis()
        {
            using var scope = await testFixture.CreateTestScope("GiveMeSomeCredit", "loans", "MonthlyIncome", this);

            await scope.MetricsTest<DistributionAnalysisComponent>(result =>
                Assert.True(result.Any()));
        }

        [Fact]
        public async void TestDescriptiveStatsPublisher()
        {
            using var scope = await testFixture.CreateTestScope("GiveMeSomeCredit", "loans", "MonthlyIncome", this);

            await scope.MetricsTest<DescriptiveStatsPublisher>(result =>
                Assert.True(result.Any()));
        }
    }
}
