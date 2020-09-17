namespace Explorer.Common.Utils
{
    using System.Collections.Immutable;
    using System.Text.Json;

    using Diffix;
    using Explorer.Common.JsonConversion;

    public class IndexedGroupingSetsResult<TIndex, TGroupedValue> : CountableRow
    {
        internal IndexedGroupingSetsResult(ref Utf8JsonReader reader, ImmutableArray<TIndex> groupingLabels)
        {
            GroupingLabels = groupingLabels;
            (GroupingId, DValue) = reader.ParseGroupingSet<TGroupedValue>(groupingLabels.Length);
            Count = reader.ParseCount();
            CountNoise = reader.ParseCountNoise();
        }

        public int GroupingId { get; }

        public TGroupedValue Value => DValue.Value;

        public long Count { get; }

        public double? CountNoise { get; }

        public ImmutableArray<TIndex> GroupingLabels { get; }

        public TIndex GroupingLabel => GroupingLabels[GroupingIndex];

        public int GroupSize => GroupingLabels.Length;

        public int GroupingIndex =>
            GroupingIdConverter.GetConverter(GroupSize).SingleIndexFromGroupingId(GroupingId);

        public bool IsNull => DValue.IsNull;

        public bool IsSuppressed => DValue.IsSuppressed;

        public bool HasValue => DValue.HasValue;

        protected DValue<TGroupedValue> DValue { get; }
    }
}