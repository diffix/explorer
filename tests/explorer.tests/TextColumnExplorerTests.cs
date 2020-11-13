namespace Explorer.Tests
{
    using System;
    using System.Linq;

    using Explorer.Components;
    using Microsoft.Extensions.Options;
    using Xunit;

    public sealed class TextColumnExplorerTests : IClassFixture<ExplorerTestFixture>
    {
        private readonly ExplorerTestFixture testFixture;

        public TextColumnExplorerTests(ExplorerTestFixture testFixture)
        {
            this.testFixture = testFixture;
        }

        [Fact]
        public async void TestLenghtDistributionLoansStatus()
        {
            using var scope = await testFixture.CreateTestScope("gda_banking", "loans", "status", this);

            var options = Options.Create(new ExplorerOptions());
            var textLenDistribution = new TextLengthDistribution(options) { Context = scope.Context };
            var r = await textLenDistribution.ComputeIsolatorLengthDistribution();
            Assert.NotNull(r);
            var d0 = r!.Distribution;
            Assert.Single(d0);
            Assert.Equal(1, d0[0].Length);

            await scope.ResultTest<TextLengthDistribution, TextLengthDistribution.Result>(r =>
            {
                Assert.NotNull(r);
                var d1 = r!.Distribution;
                Assert.Single(d1);
                Assert.Equal(1, d1[0].Length);

                Assert.True(Math.Abs(d0[0].Count - d1[0].Count) < 0.05 * d0[0].Count);
            });
        }

        [Fact]
        public async void TestLengthDistributionLoansDispType()
        {
            using var scope = await testFixture.CreateTestScope("gda_banking", "loans", "disp_type", this);

            var options = Options.Create(new ExplorerOptions());
            var textLenDistribution = new TextLengthDistribution(options) { Context = scope.Context };
            var r = await textLenDistribution.ComputeIsolatorLengthDistribution();
            Assert.NotNull(r);
            var d0 = r!.Distribution;
            Assert.Equal(2, d0.Count);
            Assert.Equal(5, d0[0].Length); // "OWNER"
            Assert.Equal(9, d0[1].Length); // "DISPONENT"

            await scope.ResultTest<TextLengthDistribution, TextLengthDistribution.Result>(r =>
            {
                Assert.NotNull(r);
                var d1 = r!.Distribution;
                Assert.Equal(2, d1.Count);
                Assert.Equal(5, d1[0].Length); // "OWNER"
                Assert.Equal(9, d1[1].Length); // "DISPONENT"

                Assert.True(Math.Abs(d0[0].Count - d1[0].Count) < 0.05 * d0[0].Count);
                Assert.True(Math.Abs(d0[1].Count - d1[1].Count) < 0.05 * d0[1].Count);
            });
        }

        [Fact]
        public async void TestClientsEmail()
        {
            using var scope = await testFixture.CreateTestScope("gda_banking", "clients", "email", this);

            await scope.ResultTest<EmailCheckComponent, EmailCheckComponent.Result>(r => Assert.True(r?.IsEmail));

            await scope.ResultTest<TextGeneratorComponent, TextGeneratorComponent.Result>(result =>
            {
                Assert.True(result?.SampleValues.All(v => v.Length >= 3));
                Assert.True(result?.SampleValues.All(v => v.Count(c => c == '@') == 1));
                Assert.True(result?.SampleValues.All(v => v.Contains('.', StringComparison.InvariantCulture)));
                Assert.True(result?.SampleValues.All(v => !v.Contains("..", StringComparison.InvariantCulture)));
            });
        }

        [Fact]
        public async void TestCardsEmail()
        {
            using var scope = await testFixture.CreateTestScope("gda_banking", "cards", "email", this);

            await scope.ResultTest<EmailCheckComponent, EmailCheckComponent.Result>(r => Assert.True(r?.IsEmail));

            await scope.ResultTest<TextGeneratorComponent, TextGeneratorComponent.Result>(result =>
            {
                Assert.True(result?.SampleValues.All(v => v.Length >= 3));
                Assert.True(result?.SampleValues.All(v => v.Count(c => c == '@') == 1));
                Assert.True(result?.SampleValues.All(v => v.Contains('.', StringComparison.InvariantCulture)));
                Assert.True(result?.SampleValues.All(v => !v.Contains("..", StringComparison.InvariantCulture)));
            });
        }

        [Fact]
        public async void TestFirstName()
        {
            using var scope = await testFixture.CreateTestScope("gda_banking", "cards", "firstname", this);

            // Make sure we use text generation
            scope.SetOptions<ExplorerOptions>(_ => _.TextColumnMinFactorForCategoricalSampling = 1.0);

            await scope.ResultTest<EmailCheckComponent, EmailCheckComponent.Result>(r => Assert.False(r?.IsEmail));

            await scope.ResultTest<TextGeneratorComponent, TextGeneratorComponent.Result>(result =>
                Assert.All(result?.SampleValues, v => Assert.True(v.Length >= 3)));
        }

        [Fact]
        public async void TestLastName()
        {
            using var scope = await testFixture.CreateTestScope("gda_banking", "cards", "lastname", this);

            // Make sure we use text generation
            scope.SetOptions<ExplorerOptions>(_ => _.TextColumnMinFactorForCategoricalSampling = 1.0);

            await scope.ResultTest<EmailCheckComponent, EmailCheckComponent.Result>(r => Assert.False(r?.IsEmail));

            await scope.ResultTest<TextGeneratorComponent, TextGeneratorComponent.Result>(result =>
                Assert.All(result?.SampleValues, v => Assert.True(v.Length >= 3)));
        }
    }
}