namespace Explorer.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Diffix;
    using Explorer.Common;
    using Explorer.Components;
    using VcrSharp;
    using Xunit;

    public sealed class TextColumnExplorerTests : IClassFixture<ExplorerTestFixture>
    {
        private readonly ExplorerTestFixture testFixture;

        public TextColumnExplorerTests(ExplorerTestFixture testFixture)
        {
            this.testFixture = testFixture;
        }

        [Fact]
        public async void TestClientsEmail()
        {
            using var scope = testFixture.SimpleComponentTestScope(
                "gda_banking",
                "clients",
                "email",
                new DColumnInfo(DValueType.Text, DColumnInfo.ColumnType.Isolating),
                Cassette.GenerateVcrFilename(this));

            await scope.ResultTest<EmailCheckComponent, EmailCheckComponent.Result>(r => Assert.True(r.IsEmail));

            await scope.ResultTest<TextGeneratorComponent, TextGeneratorComponent.Result>(result =>
            {
                Assert.True(result.SampleValues.All(v => v.Length >= 3));
                Assert.True(result.SampleValues.All(v => v.Count(c => c == '@') == 1));
                Assert.True(result.SampleValues.All(v => v.Contains('.', StringComparison.InvariantCulture)));
                Assert.True(result.SampleValues.All(v => !v.Contains("..", StringComparison.InvariantCulture)));
            });
        }

        [Fact]
        public async void TestCardsEmail()
        {
            using var scope = testFixture.SimpleComponentTestScope(
                "gda_banking",
                "cards",
                "email",
                new DColumnInfo(DValueType.Text, DColumnInfo.ColumnType.Isolating),
                Cassette.GenerateVcrFilename(this));

            await scope.ResultTest<EmailCheckComponent, EmailCheckComponent.Result>(r => Assert.True(r.IsEmail));

            await scope.ResultTest<TextGeneratorComponent, TextGeneratorComponent.Result>(result =>
            {
                Assert.True(result.SampleValues.All(v => v.Length >= 3));
                Assert.True(result.SampleValues.All(v => v.Count(c => c == '@') == 1));
                Assert.True(result.SampleValues.All(v => v.Contains('.', StringComparison.InvariantCulture)));
                Assert.True(result.SampleValues.All(v => !v.Contains("..", StringComparison.InvariantCulture)));
            });
        }

        [Fact]
        public async void TestFirstName()
        {
            using var scope = testFixture.SimpleComponentTestScope(
                "gda_banking",
                "cards",
                "firstname",
                new DColumnInfo(DValueType.Text, DColumnInfo.ColumnType.Regular),
                Cassette.GenerateVcrFilename(this));

            await scope.ResultTest<EmailCheckComponent, EmailCheckComponent.Result>(r => Assert.False(r.IsEmail));

            await scope.ResultTest<TextGeneratorComponent, TextGeneratorComponent.Result>(result =>
                Assert.True(result.SampleValues.All(v => v.Length >= 3)));
        }

        [Fact]
        public async void TestLastName()
        {
            using var scope = testFixture.SimpleComponentTestScope(
                "gda_banking",
                "cards",
                "lastname",
                new DColumnInfo(DValueType.Text, DColumnInfo.ColumnType.Isolating),
                Cassette.GenerateVcrFilename(this));

            await scope.ResultTest<EmailCheckComponent, EmailCheckComponent.Result>(r => Assert.False(r.IsEmail));

            await scope.ResultTest<TextGeneratorComponent, TextGeneratorComponent.Result>(result =>
                Assert.True(result.SampleValues.All(v => v.Length >= 3)));
        }
    }
}