namespace Explorer.Tests
{
    using System.Linq;
    using System.Threading.Tasks;

    using Explorer.Queries;
    using Xunit;

    public sealed class MultiColumnQueryTests : IClassFixture<ExplorerTestFixture>
    {
        private readonly ExplorerTestFixture testFixture;

        public MultiColumnQueryTests(ExplorerTestFixture testFixture)
        {
            this.testFixture = testFixture;
        }

        [Fact]
        public async Task TestMultiColumnCategories()
        {
            using var testScope = await testFixture.CreateTestScope(
                "gda_banking",
                "loans",
                new[] { "disp_type", "status", "duration" },
                this);

            var query = new ColumnPairs();

            var rows = await testScope.QueryRows(query);

            Assert.All(rows, row =>
            {
                Assert.True(row.GroupingLabels.Count() == 2);
                Assert.True(row.Values.Length == 2);
            });
        }
    }
}