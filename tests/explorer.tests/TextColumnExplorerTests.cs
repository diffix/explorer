namespace Explorer.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Explorer.Components;
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
            using var scope = testFixture.SimpleComponentTestScope("gda_banking", "clients", "email");

            await scope.Test<EmailCheckComponent, bool>(Assert.True);

            await scope.Test<TextGeneratorComponent, IEnumerable<string>>(result =>
            {
                Assert.True(result.All(v => v.Length >= 3));
                Assert.True(result.All(v => v.Count(c => c == '@') == 1));
                Assert.True(result.All(v => v.Contains('.', StringComparison.InvariantCulture)));
                Assert.True(result.All(v => !v.Contains("..", StringComparison.InvariantCulture)));
            });
        }

        [Fact]
        public async void TestCardsEmail()
        {
            using var scope = testFixture.SimpleComponentTestScope("gda_banking", "cards", "email");

            await scope.Test<EmailCheckComponent, bool>(Assert.True);

            await scope.Test<TextGeneratorComponent, IEnumerable<string>>(result =>
            {
                Assert.True(result.All(v => v.Length >= 3));
                Assert.True(result.All(v => v.Count(c => c == '@') == 1));
                Assert.True(result.All(v => v.Contains('.', StringComparison.InvariantCulture)));
                Assert.True(result.All(v => !v.Contains("..", StringComparison.InvariantCulture)));
            });
        }

        [Fact]
        public async void TestFirstName()
        {
            using var scope = testFixture.SimpleComponentTestScope("gda_banking", "cards", "firstname");

            await scope.Test<EmailCheckComponent, bool>(Assert.False);

            await scope.Test<TextGeneratorComponent, IEnumerable<string>>(result =>
                Assert.True(result.All(v => v.Length >= 3)));
        }

        [Fact]
        public async void TestLastName()
        {
            using var scope = testFixture.SimpleComponentTestScope("gda_banking", "cards", "lastname");

            await scope.Test<EmailCheckComponent, bool>(Assert.False);

            await scope.Test<TextGeneratorComponent, IEnumerable<string>>(result =>
                Assert.True(result.All(v => v.Length >= 3)));
        }
    }
}