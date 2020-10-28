namespace Explorer.Common.Tests
{
    using System;
    using System.Linq;
    using Explorer.Common;
    using Xunit;

    public sealed class PermutationsEnumeratorTests
    {
        [Fact]
        public void EmptyDoesNothing()
        {
            var empty = Array.Empty<int>();

            var result = empty.EnumeratePermutations(10);

            Assert.Empty(result);
        }

        [Fact]
        public void ListsAllPermutationsOnce()
        {
            var sourceList = new[] { 'A', 'B', 'C', 'D' };

            var expectedPermutations = new[]
                {
                    new[] { 'A' },
                    new[] { 'B' },
                    new[] { 'C' },
                    new[] { 'D' },
                    new[] { 'A', 'B' },
                    new[] { 'A', 'C' },
                    new[] { 'A', 'D' },
                    new[] { 'B', 'C' },
                    new[] { 'B', 'D' },
                    new[] { 'C', 'D' },
                    new[] { 'A', 'B', 'C' },
                    new[] { 'A', 'B', 'D' },
                    new[] { 'A', 'C', 'D' },
                    new[] { 'A', 'B', 'C', 'D' },
                };

            var results = sourceList.EnumeratePermutations(10).ToArray();

            Assert.Equal(expectedPermutations.Length, results.Length);
            foreach (var permutation in expectedPermutations)
            {
                Assert.Contains(permutation, results);
            }
        }

        [Fact]
        public void ListsSubsetOfPermutations()
        {
            var sourceList = new[] { 'A', 'B', 'C', 'D' };

            var expectedPermutations = new[]
                {
                    new[] { 'A' },
                    new[] { 'B' },
                    new[] { 'C' },
                    new[] { 'D' },
                    new[] { 'A', 'B' },
                    new[] { 'A', 'C' },
                    new[] { 'A', 'D' },
                    new[] { 'B', 'C' },
                    new[] { 'B', 'D' },
                    new[] { 'C', 'D' },
                };

            var results = sourceList.EnumeratePermutations(2).ToArray();

            Assert.Equal(expectedPermutations.Length, results.Length);
            foreach (var permutation in expectedPermutations)
            {
                Assert.Contains(permutation, results);
            }
        }
    }
}