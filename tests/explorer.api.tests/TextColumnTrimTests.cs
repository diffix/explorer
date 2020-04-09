namespace Explorer.Api.Tests
{
    using System.Linq;
    using Explorer.Common;
    using Explorer.Explorers;
    using Explorer.Queries;
    using Xunit;

    public sealed class TextColumnTrimTests : IClassFixture<TestWebAppFactory>
    {
        private const string TestDataSource = "gda_banking";

        private readonly TestWebAppFactory factory;

        public TextColumnTrimTests(TestWebAppFactory factory)
        {
            this.factory = factory;
        }

        [Fact]
        public async void TestEmailPositive()
        {
            var query = await factory.QueryResult(
                new TextColumnTrim("clients", "email", TextColumnTrimType.Both, EmailColumnExplorer.EmailAddressChars),
                TestDataSource,
                nameof(TextColumnTrimTests));

            var counts = ValueCounts.Compute(query.Rows);

            var isEmail = counts.TotalCount == query.Rows
                .Where(r => r.IsNull || r.Value == "@")
                .Sum(r => r.Count);

            Assert.True(isEmail);
        }

        [Fact]
        public async void TestEmailNegative()
        {
            var query = await factory.QueryResult(
                new TextColumnTrim("cards", "lastname", TextColumnTrimType.Both, EmailColumnExplorer.EmailAddressChars),
                TestDataSource,
                nameof(TextColumnTrimTests));

            var counts = ValueCounts.Compute(query.Rows);

            var isEmail = counts.TotalCount == query.Rows
                .Where(r => r.IsNull || r.Value == "@")
                .Sum(r => r.Count);

            Assert.False(isEmail);
        }
    }
}