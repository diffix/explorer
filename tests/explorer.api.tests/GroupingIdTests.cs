namespace Explorer.Queries.Tests
{
    using System;
    using System.Linq;
    using Explorer.Queries;
    using Xunit;

    public sealed class GroupingIdTests
    {
        [Theory]
        [InlineData(33)]
        [InlineData(-1)]
        [InlineData(0)]
        public void FailsWithInvalidGroupSize(int groupSize)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new GroupingIdConverter(groupSize));
        }

        [Theory]
        [InlineData(4, -1)] // negative index
        [InlineData(4, 4)] // out of bounds
        public void FailsWithOutOfBoundsIndices(int groupSize, int invalidValue)
        {
            var converter = new GroupingIdConverter(groupSize);
            Assert.Throws<ArgumentOutOfRangeException>(() => converter.GroupingIdFromIndex(invalidValue));
        }

        [Theory]
        [InlineData(4, 0b1111)] // no value included in group
        [InlineData(4, 0b0101)] // multiple values in group
        [InlineData(4, 0b0000)] // all values in group
        public void FailsWithInvalidGroupingId(int groupSize, int invalidValue)
        {
            var converter = new GroupingIdConverter(groupSize);
            Assert.Throws<InvalidOperationException>(() => converter.SingleIndexFromGroupingId(invalidValue));
        }

        [Theory]
        [InlineData(4, 0b101111)] // out of bounds grouping id, index also out of bounds
        [InlineData(4, 0b111110)] // out of bounds grouping id, index in bounds
        [InlineData(4, -1)] // out of bounds grouping id, index in bounds
        public void FailsWithOutOfBoundsGroupingId(int groupSize, int invalidValue)
        {
            var converter = new GroupingIdConverter(groupSize);
            Assert.Throws<ArgumentOutOfRangeException>(() => converter.SingleIndexFromGroupingId(invalidValue));
        }

        [Theory]
        [InlineData(4, 0, 0b0111)]
        [InlineData(4, 1, 0b1011)]
        [InlineData(4, 2, 0b1101)]
        [InlineData(4, 3, 0b1110)]
        [InlineData(7, 0, 63)]
        [InlineData(7, 1, 95)]
        [InlineData(7, 2, 111)]
        public void ReturnsCorrectSingleGroupingId(int groupSize, int index, int expectedGroupingId)
        {
            var converter = new GroupingIdConverter(groupSize);

            Assert.Equal(expectedGroupingId, converter.GroupingIdFromIndex(index));
        }

        [Theory]
        [InlineData(4, 0b1111, new int[] { })]
        [InlineData(4, 0b0000, new int[] { 0, 1, 2, 3 })]
        [InlineData(4, 0b0101, new int[] { 0, 2 })]
        [InlineData(4, 0b0110, new int[] { 0, 3 })]
        [InlineData(4, 0b0111, new int[] { 0 })]
        public void ReturnsCorrectIndices(int groupSize, int groupingId, int[] expectedIndices)
        {
            var converter = new GroupingIdConverter(groupSize);

            Assert.Equal(expectedIndices, converter.IndicesFromGroupingId(groupingId).ToArray());
        }
    }
}