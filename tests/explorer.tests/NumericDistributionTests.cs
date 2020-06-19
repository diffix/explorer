namespace Explorer.Tests
{
    using System.Linq;

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
                ExplorerTestFixture.GenerateVcrFilename(this));

            await scope.Test<NumericSampleGenerator>(result =>
            {
                // TODO: Check metrics against schema definition
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
                ExplorerTestFixture.GenerateVcrFilename(this));

            await scope.Test<DistributionAnalysisComponent>(result =>
            {
                Assert.True(result.Any());
            });
        }

        [Fact]
        public async void TestDescriptiveStatsPublisher()
        {
            using var scope = testFixture.SimpleComponentTestScope(
                "GiveMeSomeCredit",
                "loans",
                "age",
                ExplorerTestFixture.GenerateVcrFilename(this));

            await scope.Test<DescriptiveStatsPublisher>(result =>
            {
                Assert.True(result.Any());
            });
        }
    }
}
