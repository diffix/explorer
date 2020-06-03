namespace Explorer.Tests
{
    using System.Linq;

    using Accord.Statistics.Distributions.Univariate;
    using Explorer.Components;
    using Xunit;

    public class NumericSampleGeneratorTest : IClassFixture<ExplorerTestFixture>
    {
        private readonly ExplorerTestFixture testFixture;

        public NumericSampleGeneratorTest(ExplorerTestFixture testFixture)
        {
            this.testFixture = testFixture;
        }

        [Fact]
        public async void TestEmpiricalDistributionGenerator()
        {
            using var scope = testFixture.SimpleComponentTestScope(
                "gda_banking",
                "loans",
                "duration",
                ExplorerTestFixture.GenerateVcrFilename(this));

            await scope.Test<EmpiricalDistributionComponent, EmpiricalDistribution>(result =>
            {
                Assert.True(result.Length > 0);
            });
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
                Assert.True(result.Any());
            });
        }
    }
}
