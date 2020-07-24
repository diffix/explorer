namespace Explorer.Tests
{
    using System.Linq;

    using Diffix;
    using Explorer.Common;
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
            using var queryScope = testFixture.SimpleQueryTestScope(
                TestDataSource,
                VcrSharp.Cassette.GenerateVcrFilename(this));

            var result = await queryScope.QueryRows(
                new TextColumnTrim(
                    new DSqlObjectName("clients"),
                    new DSqlObjectName("email"),
                    TextColumnTrimType.Both,
                    Constants.EmailAddressChars));

            var counts = ValueCounts.Compute(result);

            var isEmail = counts.TotalCount == result
                .Where(r => r.IsNull || r.Value == "@")
                .Sum(r => r.Count);

            Assert.True(isEmail);
        }

        [Fact]
        public async void TestEmailNegative()
        {
            using var queryScope = testFixture.SimpleQueryTestScope(
                TestDataSource,
                VcrSharp.Cassette.GenerateVcrFilename(this));

            var result = await queryScope.QueryRows(
                new TextColumnTrim(
                    new DSqlObjectName("cards"),
                    new DSqlObjectName("lastname"),
                    TextColumnTrimType.Both,
                    Constants.EmailAddressChars));

            var counts = ValueCounts.Compute(result);

            var isEmail = counts.TotalCount == result
                .Where(r => r.IsNull || r.Value == "@")
                .Sum(r => r.Count);

            Assert.False(isEmail);
        }
    }
}