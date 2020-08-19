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

        public bool IsNull => Values.All(v => v.IsNull);

        public bool IsSuppressed => Values.Any(v => v.IsSuppressed);

        private ImmutableArray<TIndex> AllGroupingLabels { get; }
    }
}