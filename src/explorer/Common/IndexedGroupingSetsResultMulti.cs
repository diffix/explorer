namespace Explorer.Common
{
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;
    using System.Text.Json;

    using Diffix;
    using Explorer.JsonExtensions;

    public class IndexedGroupingSetsResultMulti<TIndex> : CountableRow
    {
        internal IndexedGroupingSetsResultMulti(ref Utf8JsonReader reader, ImmutableArray<TIndex> groupingLabels)
        {
            AllGroupingLabels = groupingLabels;
            (GroupingId, Values) = reader.ParseMultiGroupingSet(groupingLabels.Length);
            Count = reader.ParseCount();
            CountNoise = reader.ParseCountNoise();
        }

        public int GroupingId { get; }

        public ImmutableArray<DValue<JsonElement>> Values { get; }

        public long Count { get; }

        public double? CountNoise { get; }

        public IEnumerable<TIndex> GroupingLabels
            => AllGroupingLabels.Where((_, index) => GroupingIndices.Contains(index));

        public IEnumerable<int> GroupingIndices =>
            GroupingIdConverter.GetConverter(AllGroupingLabels.Length).IndicesFromGroupingId(GroupingId);

        /// <summary>
        /// Gets a value indicating whether the row should be considered `null`. The row is considered null if *all*
        /// columns are null.
        /// <para>
        /// Rationale:
        /// Null is like nothing.
        /// If all columns have nothing, then the row has nothing.
        /// But if any column has something (not null) then the row has something.
        /// </para>
        /// </summary>
        /// <returns><c>true</c> if all columns are null, otherwise <c>false</c>.</returns>
        public bool IsNull => Values.All(v => v.IsNull);

        /// <summary>
        /// Gets a value indicating whether the row should be considered suppressed. The row is considered suppressed
        /// if *any* column is suppressed.
        /// <para>
        /// Rationale:
        /// Suppressed values are uncertain.
        /// If any column is uncertain than that makes the row uncertain.
        /// Only if all columns are certain (not suppressed) can the row be considered certain.
        /// </para>
        /// </summary>
        /// <returns><c>true</c> if any column is suppressed, otherwise <c>false</c>.</returns>
        public bool IsSuppressed => Values.Any(v => v.IsSuppressed);

        private ImmutableArray<TIndex> AllGroupingLabels { get; }
    }
}