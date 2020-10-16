namespace Explorer.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Text.Json;
    using System.Threading.Tasks;
    using Diffix;
    using Explorer.Components;
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
            var columns = new[] { "disp_type", "status", "duration" };
            var groupSize = columns.Length;

            using var testScope = await testFixture.CreateTestScope(
                "gda_banking",
                "loans",
                columns,
                this);

            var projections = columns.Select((c, i) => new IdentityProjection(c, i, Diffix.DValueType.Text));
            var query = new MultiColumnCounts(projections);

            var rows = await testScope.QueryRows(query);

            Assert.All(rows, row =>
            {
                var numLabels = row.GroupingLabels.Count();
                Assert.True(numLabels <= groupSize);
                Assert.True(row.Values.Length == numLabels);
            });
        }

        [Fact]
        public async Task MultiColumnCountsFromCompositeQuery()
        {
            var columns = new[] { "disp_type", "status", "duration" };

            using var testScope = await testFixture.CreateTestScope(
                "gda_banking",
                "loans",
                columns,
                this);

            var projections = columns.Select((c, i) => new IdentityProjection(c, i, Diffix.DValueType.Text));

            var referenceRows = await testScope.QueryRows(new MultiColumnCounts(projections));

            var compositeRows = await ColumnCorrelationComponent.DrillDown(
                testScope.Context,
                projections);

            Assert.All(compositeRows, row =>
            {
                var numLabels = row.GroupingLabels.Count();
                Assert.True(numLabels <= columns.Length);
                Assert.True(row.Values.Length == numLabels);
            });

            var joined = referenceRows.GroupBy(_ => _.ColumnGrouping)
                .Join(
                    compositeRows.GroupBy(_ => _.ColumnGrouping),
                    _ => _.Key,
                    _ => _.Key,
                    (reference, composite) => (Expected: reference, Actual: composite));

            Assert.All(joined, joinedResults =>
            {
                var (expected, actual) = joinedResults;
                Assert.Equal(expected.Count(), actual.Count());
                Assert.Equal(
                    expected.OrderBy(_ => _.Values, JsonElementByValueComparer.Instance).AsEnumerable(),
                    actual.OrderBy(_ => _.Values, JsonElementByValueComparer.Instance).AsEnumerable(),
                    MultiColumnCountsResultComparer.Instance);
            });
        }

        private class JsonElementByValueComparer :
            IComparer<IEnumerable<DValue<JsonElement>>>
        {
            private JsonElementByValueComparer()
            {
            }

            public static JsonElementByValueComparer Instance { get; } = new JsonElementByValueComparer();

            public int Compare(
                [AllowNull] IEnumerable<DValue<JsonElement>> x,
                [AllowNull] IEnumerable<DValue<JsonElement>> y) => string.CompareOrdinal(
                    string.Concat(x.Select(_ => _.ToString())),
                    string.Concat(y.Select(_ => _.ToString())));
        }

        private class MultiColumnCountsResultComparer :
            IEqualityComparer<MultiColumnCounts.Result>
        {
            private MultiColumnCountsResultComparer()
            {
            }

            public static MultiColumnCountsResultComparer Instance { get; } = new MultiColumnCountsResultComparer();

            public bool Equals(
                [AllowNull] MultiColumnCounts.Result x,
                [AllowNull] MultiColumnCounts.Result y)
                => x is MultiColumnCounts.Result
                && y is MultiColumnCounts.Result
                && x.Count == y.Count
                && x.Values.SequenceEqual(y.Values, DValueJsonElementComparer.Instance);

            public int GetHashCode([DisallowNull] MultiColumnCounts.Result obj)
                => HashCode.Combine(obj.Values.Select(v => v.GetHashCode()));
        }

        private class DValueJsonElementComparer : IEqualityComparer<DValue<JsonElement>>
        {
            private DValueJsonElementComparer()
            {
            }

            public static DValueJsonElementComparer Instance { get; } = new DValueJsonElementComparer();

            public bool Equals([AllowNull] DValue<JsonElement> x, [AllowNull] DValue<JsonElement> y)
                => string.Equals(x?.ToString(), y?.ToString(), StringComparison.Ordinal);

            public int GetHashCode([DisallowNull] DValue<JsonElement> obj)
                => obj.ToString()?.GetHashCode(StringComparison.Ordinal) ?? 0;
        }
    }
}