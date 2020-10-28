namespace Explorer.Common
{
    using System;
    using System.Collections.Immutable;
    using System.Diagnostics.CodeAnalysis;

    public struct ColumnGrouping : IEquatable<ColumnGrouping>
    {
        private readonly int numColumns;

        public ColumnGrouping(int numColumns, int groupingId)
        {
            this.numColumns = numColumns;
            GroupingId = groupingId;
            Indices = GroupingIdConverter.GetConverter(numColumns).IndicesFromGroupingId(GroupingId).ToImmutableArray();
        }

        public int GroupingId { get; }

        public ImmutableArray<int> Indices { get; }

        public static bool operator ==(ColumnGrouping left, ColumnGrouping right) => left.Equals(right);

        public static bool operator !=(ColumnGrouping left, ColumnGrouping right) => !(left == right);

        public override bool Equals(object? obj)
            => obj is ColumnGrouping other
            && other.Equals(this);

        public override int GetHashCode()
            => HashCode.Combine(numColumns, GroupingId);

        public bool Equals([AllowNull] ColumnGrouping other)
            => other.GroupingId == GroupingId
            && other.numColumns == numColumns;
    }
}
