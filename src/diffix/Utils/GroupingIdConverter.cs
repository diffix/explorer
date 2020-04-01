namespace Diffix.Utils
{
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;

    public sealed class GroupingIdConverter
    {
        private static ImmutableDictionary<int, GroupingIdConverter> converters =
            ImmutableDictionary.Create<int, GroupingIdConverter>();

        private readonly int groupMask;

        private readonly int groupSize;

        private GroupingIdConverter(int groupSize)
        {
            if (groupSize < 1 || groupSize > sizeof(int) * 8)
            {
                throw new System.ArgumentOutOfRangeException(
                    $"Group size must be in range [1 {sizeof(int) * 8}].");
            }

            var mask = 0;
            for (var i = 0; i < groupSize; i++)
            {
                mask |= 1 << i;
            }

            groupMask = mask;
            this.groupSize = groupSize;
        }

        public static GroupingIdConverter GetConverter(int groupSize)
        {
            return ImmutableInterlocked
                .GetOrAdd(ref converters, groupSize, size => new GroupingIdConverter(size));
        }

        public int GroupingIdFromIndex(int index)
        {
            if (index >= groupSize || index < 0)
            {
                throw new System.ArgumentOutOfRangeException(
                    $"Index {index} is not within valid range [0 {groupSize}].");
            }

            return (1 << (groupSize - index - 1)) ^ groupMask;
        }

        public int SingleIndexFromGroupingId(int groupingId)
        {
            return IndicesFromGroupingId(groupingId).Single();
        }

        public IEnumerable<int> IndicesFromGroupingId(int groupingId)
        {
            if (groupingId < 0 || groupingId > groupMask)
            {
                throw new System.ArgumentOutOfRangeException(
                    $"Grouping id must be in range [0 {groupMask}]. Got {groupingId}.");
            }

            return IndexIterator();

            IEnumerable<int> IndexIterator()
            {
                var onesMask = groupingId ^ groupMask;
                for (var i = 0; i < groupSize; i++)
                {
                    var offset = groupSize - 1 - i;
                    if ((1 << offset & onesMask) > 0)
                    {
                        yield return i;
                    }
                }
            }
        }
    }
}