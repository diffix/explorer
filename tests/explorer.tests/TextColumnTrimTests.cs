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
        private readonly ExplorerTestFixture testFixture;

        public TextColumnTrimTests(ExplorerTestFixture testFixture)
        {
            this.testFixture = testFixture;
        }

        [Fact]
        public async void TestEmailPositive()
        {
            using var queryScope = testFixture.SimpleQueryTestScope(TestDataSource);

            var result = await queryScope.QueryRows(
                new TextColumnTrim("clients", "email", TextColumnTrimType.Both, TextUtilities.EmailAddressChars));

            var counts = ValueCounts.Compute(result);

            var isEmail = counts.TotalCount == result
                .Where(r => r.IsNull || r.Value == "@")
                .Sum(r => r.Count);

            Assert.True(isEmail);
        }

        [Fact]
        public async void TestEmailNegative()
        {
            using var queryScope = testFixture.SimpleQueryTestScope(TestDataSource);

            var result = await queryScope.QueryRows(
                new TextColumnTrim("cards", "lastname", TextColumnTrimType.Both, TextUtilities.EmailAddressChars));

            var counts = ValueCounts.Compute(result);

            var isEmail = counts.TotalCount == result
                .Where(r => r.IsNull || r.Value == "@")
                .Sum(r => r.Count);

            Assert.False(isEmail);
        }
    }
}