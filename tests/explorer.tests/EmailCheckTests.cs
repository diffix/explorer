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
            await scope.ResultTest<EmailCheckComponent, EmailCheckComponent.Result>(result => Assert.True(result?.IsEmail));
        }

        [Fact]
        public async void TestEmailNegative()
        {
            using var scope = await testFixture.CreateTestScope("gda_banking", "cards", "lastname", this);
            await scope.ResultTest<EmailCheckComponent, EmailCheckComponent.Result>(result => Assert.False(result?.IsEmail));
        }
    }
}