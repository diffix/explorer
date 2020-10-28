namespace Explorer.Common.Tests
{
    using System;
    using System.Linq;
    using Explorer.Common;
    using Xunit;

    public sealed class PairsEnumeratorTests
    {
        [Fact]
        public void EmptyDoesNothing()
        {
            var empty = Array.Empty<int>();

            var emptyPairs = empty.EnumeratePairs();

            Assert.Empty(emptyPairs);
        }

        [Fact]
        public void ListsAllPairsOnce()
        {
            var sourceList = new[] { 'A', 'B', 'C', 'D' };

            var expectedPairs = new[]
                {
                    ('A', 'B'),
                    ('A', 'C'),
                    ('A', 'D'),
                    ('B', 'C'),
                    ('B', 'D'),
                    ('C', 'D'),
                };

            var resultPairs = sourceList.EnumeratePairs().ToArray();

            Assert.Equal(resultPairs.Length, expectedPairs.Length);
            foreach (var pair in expectedPairs)
            {
                Assert.Contains(pair, resultPairs);
            }
        }
    }
}