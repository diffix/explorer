namespace Explorer.Tests
{
    using System;
    using System.Linq;

    using Explorer.Common.Utils;
    using Xunit;

    public sealed class ValueWithCountListTests
    {
        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(4)]
        [InlineData(5)]
        [InlineData(6)]
        [InlineData(7)]
        [InlineData(8)]
        [InlineData(9)]
        [InlineData(10)]
        [InlineData(11)]
        [InlineData(12)]
        [InlineData(13)]
        [InlineData(14)]
        [InlineData(15)]
        public void FindValueTest(int count)
        {
            var vals = ValueWithCountList<int>.FromValueWithCountEnum(
                Enumerable.Range(0, count).Select(i => ValueWithCount<int>.ValueCount(i, 1L)));
            for (var i = 0; i < vals.Count; i++)
            {
                Assert.Equal(i, vals.FindValue(i));
            }
            Assert.Throws<ArgumentException>(() => vals.FindValue(count));
            Assert.Throws<ArgumentException>(() => vals.FindValue(count + 1));
        }

        [Fact]
        public void FindValueFromEmptyList()
        {
            var vals = ValueWithCountList<string>.FromValueWithCountEnum(Enumerable.Empty<ValueWithCount<string>>());
            Assert.Throws<InvalidOperationException>(() => vals.FindValue(1));
        }
    }
}
