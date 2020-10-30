namespace Explorer.Common
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Diagnostics;
    using System.Linq;
    using System.Numerics;

    /// <summary>
    /// GroupingIdConverter is a utility class for converting between a grouping_id interger and the group indices
    /// represented thereby
    /// (see https://attack.aircloak.com/docs/sql/functions.html#special-functions).
    /// </summary>
    /// <remarks>
    /// The <c>grouping_id</c> function returns a bitmask whereby bits are assigned with the rightmost argument being
    /// the least-significant bit; each bit is 0 if the corresponding expression is included in the grouping criteria of
    /// the grouping set generating the result row, and 1 if it is not.
    /// </remarks>
    internal sealed class GroupingIdConverter
    {
        /// <summary>
        /// This is the maximum number of columns that can be included in a grouping statement. For most DBs this is
        /// 32 (the number of bits in an int).
        /// </summary>
        public const int GroupSizeLimit = 32;

        private static ImmutableDictionary<int, GroupingIdConverter> converters =
            ImmutableDictionary.Create<int, GroupingIdConverter>();

        private readonly int groupMask;

        private readonly int groupSizeMax;

        private GroupingIdConverter(int groupSizeMax)
        {
            if (groupSizeMax < 1 || groupSizeMax >= GroupSizeLimit)
            {
                throw new ArgumentOutOfRangeException($"Group size must be in range [1 {GroupSizeLimit}].");
            }

            var mask = 0;
            for (var i = 0; i < groupSizeMax; i++)
            {
                mask |= 1 << i;
            }

            groupMask = mask;
            this.groupSizeMax = groupSizeMax;
        }

        /// <summary>
        /// Initialises a converter for a grouping id for a given maximum group size.
        /// </summary>
        /// <param name="groupSizeMax">The total number of grouped columns in the sql call to grouping_id().</param>
        /// <returns>A <see cref="GroupingIdConverter" /> for the correct group size.</returns>
        public static GroupingIdConverter GetConverter(int groupSizeMax)
        {
            return ImmutableInterlocked
                .GetOrAdd(ref converters, groupSizeMax, size => new GroupingIdConverter(size));
        }

        /// <summary>
        /// Returns the grouping id corresponding to a single-value group of the value at a given index.
        /// <para>
        /// For example, for columns (A, B, C) the grouping id corresponding to a group containing only column B would
        /// be <c>GroupingIdFromIndex(1)</c>.
        /// </para>
        /// </summary>
        /// <param name="index">Zero-based index of the desired element.</param>
        /// <returns>The grouping id as an integer.</returns>
        public int GroupingIdFromIndex(int index)
        {
            Debug.Assert(
                index < groupSizeMax && index >= 0,
                $"Index {index} is not within valid range [0 {groupSizeMax}].");

            return (1 << (groupSizeMax - index - 1)) ^ groupMask;
        }

        /// <summary>
        /// Returns the grouping id corresponding to a list of grouped elements represented by their indices.
        /// <para>
        /// For example, for columns (A, B, C) the grouping id corresponding to a grouping ofcolumns A and B would
        /// be <c>GroupingIdFromIndices(0, 1)</c>.
        /// </para>
        /// </summary>
        /// <param name="indices">Indices of the elements included in the group.</param>
        /// <returns>The grouping id that includes the desired indices.</returns>
        public int GroupingIdFromIndices(params int[] indices)
            => indices
                .Select(i => GroupingIdFromIndex(i))
                .Aggregate(groupMask, (agg, grouping_id) => agg & grouping_id);

        /// <summary>
        /// Convenience method to extract the index encoded by a grouping id that encodes a single-element group.
        /// </summary>
        /// <param name="groupingId">The grouping id.</param>
        /// <returns>The index encoded by the grouping id.</returns>
        public int SingleIndexFromGroupingId(int groupingId)
        {
            return IndicesFromGroupingId(groupingId).Single();
        }

        /// <summary>
        /// Converts a grouping id to a list of indices.
        /// <para>
        /// For example, for columns (A, B, C), the grouping id 0b001 is corresponds to the subgroup (A, B). So
        /// <c>IndicesFromGroupingId(0b001)</c> would yield the indices of these columns: <c>{ 0, 1 }</c>.
        /// </para>
        /// </summary>
        /// <param name="groupingId">The grouping id.</param>
        /// <returns>The indices encoded by the grouping id.</returns>
        /// <exception cref="System.ArgumentOutOfRangeException">
        /// Thrown if the grouping id is not within the range of the group ([0 groupSize]).
        /// </exception>
        public IEnumerable<int> IndicesFromGroupingId(int groupingId)
        {
            Debug.Assert(
                groupingId >= 0 && groupingId <= groupMask,
                $"Grouping id must be in range [0 {groupMask}]. Got {groupingId}.");

            return IndexIterator();

            IEnumerable<int> IndexIterator()
            {
                var onesMask = groupingId ^ groupMask;
                for (var i = 0; i < groupSizeMax; i++)
                {
                    var offset = groupSizeMax - 1 - i;
                    if ((1 << offset & onesMask) > 0)
                    {
                        yield return i;
                    }
                }
            }
        }

        /// <summary>
        /// Calculates the size of a subgroup based on the <c>groupingId</c>.
        /// For example, the groupingID <c>0b010</c> for columns (A, B, C) represents the subgroup (A,C). The size of
        /// this subgroup is 2.
        /// </summary>
        /// <param name="groupingId">The groupingId.</param>
        /// <returns>The size of the group represented by the groupingId.</returns>
        public int SubGroupSize(int groupingId) => groupSizeMax - BitOperations.PopCount((uint)groupingId);
    }
}