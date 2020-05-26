namespace Explorer.Tests
{
    using System.Linq;
    using Explorer.Common;
    using Explorer.Explorers;
    using Explorer.Queries;
    using Xunit;

    public sealed class TextColumnTrimTests : IClassFixture<ExplorerTestFixture>
    {
        private const string TestDataSource = "gda_banking";

        private readonly QueryableTestScope queryScope;

        public TextColumnTrimTests(ExplorerTestFixture testFixture)
        {
            queryScope = testFixture.SimpleQueryTestScope(TestDataSource);
        }

        [Fact]
        public async void TestEmailPositive()
        {
            var result = await queryScope.QueryRows(
                new TextColumnTrim("clients", "email", TextColumnTrimType.Both, TextColumnExplorer.EmailAddressChars));

            var counts = ValueCounts.Compute(result);

            var isEmail = counts.TotalCount == result
                .Where(r => r.IsNull || r.Value == "@")
                .Sum(r => r.Count);

            Assert.True(isEmail);
        }

        [Fact]
        public async void TestEmailNegative()
        {
            var result = await queryScope.QueryRows(
                new TextColumnTrim("cards", "lastname", TextColumnTrimType.Both, TextColumnExplorer.EmailAddressChars));

            var counts = ValueCounts.Compute(result);

            var isEmail = counts.TotalCount == result
                .Where(r => r.IsNull || r.Value == "@")
                .Sum(r => r.Count);

            Assert.False(isEmail);
        }
    }
}