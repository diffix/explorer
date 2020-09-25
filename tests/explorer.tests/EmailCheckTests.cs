namespace Explorer.Tests
{
    using Explorer.Components;
    using Xunit;

    public sealed class EmailCheckTests : IClassFixture<ExplorerTestFixture>
    {
        private readonly ExplorerTestFixture testFixture;

        public EmailCheckTests(ExplorerTestFixture testFixture)
        {
            this.testFixture = testFixture;
        }

        [Fact]
        public async void TestEmailPositive()
        {
            using var scope = await testFixture.CreateTestScope("gda_banking", "clients", "email", this);
            await scope.ResultTest<TextFormatDetectorComponent, TextFormatDetectorComponent.Result>(result => Assert.True(result?.TextFormat == Metrics.TextFormat.Email));
        }

        [Fact]
        public async void TestEmailNegative()
        {
            using var scope = await testFixture.CreateTestScope("gda_banking", "cards", "lastname", this);
            await scope.ResultTest<TextFormatDetectorComponent, TextFormatDetectorComponent.Result>(result => Assert.False(result?.TextFormat == Metrics.TextFormat.Email));
        }
    }
}