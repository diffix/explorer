namespace Explorer.Api.Tests
{
    using System;
    using Explorer.Queries;
    using Xunit;

    public sealed class BucketSizesTests
    {
        [Fact]
        public void FailsForInvalidValues()
        {
            Assert.Throws<ArgumentException>(() => new BucketSize(-0.1M));
            Assert.Throws<ArgumentException>(() => new BucketSize(0M));
        }

        [Fact]
        public void TestSmallValue()
        {
            var b = new BucketSize(0.000001M);
            Assert.Equal(0.0001M, b.SnappedSize);
        }

        [Fact]
        public void TestBigValue()
        {
            var b = new BucketSize(6_000_000_000_000M);
            Assert.Equal(5_000_000_000_000M, b.SnappedSize);
        }

        [Fact]
        public void TestSnapFirst()
        {
            var b = new BucketSize(0.00014M);
            Assert.Equal(0.0001M, b.SnappedSize);
        }

        [Fact]
        public void TestSnapSecond()
        {
            var b = new BucketSize(0.00016M);
            Assert.Equal(0.0002M, b.SnappedSize);
        }

        [Fact]
        public void TestSnapLast()
        {
            var b = new BucketSize(4_000_000_000_000M);
            Assert.Equal(5_000_000_000_000M, b.SnappedSize);
        }

        [Fact]
        public void TestSnapSecondLast()
        {
            var b = new BucketSize(3_000_000_000_000M);
            Assert.Equal(2_000_000_000_000M, b.SnappedSize);
        }

        [Fact]
        public void TestSnap100()
        {
            var b = new BucketSize(120M);
            Assert.Equal(100M, b.SnappedSize);
        }

        [Fact]
        public void TestSnap200()
        {
            var b = new BucketSize(150);
            Assert.Equal(200M, b.SnappedSize);
        }

        [Fact]
        public void TestLarger()
        {
            var b = new BucketSize(1_300);
            Assert.Equal(1_000M, b.SnappedSize);
            Assert.Equal(5_000M, b.Larger(2).SnappedSize);
        }

        [Fact]
        public void TestSmaller()
        {
            var b = new BucketSize(80);
            Assert.Equal(100M, b.SnappedSize);
            Assert.Equal(20M, b.Smaller(2).SnappedSize);
        }

        [Fact]
        public void TestLargerOutOfBounds()
        {
            var b = new BucketSize(3_300_000_000_000M);
            Assert.Equal(2_000_000_000_000M, b.SnappedSize);
            Assert.Equal(5_000_000_000_000M, b.Larger(2).SnappedSize);
        }

        [Fact]
        public void TestSmallerOutOfBounds()
        {
            var b = new BucketSize(0.00022M);
            Assert.Equal(0.0002M, b.SnappedSize);
            Assert.Equal(0.0001M, b.Smaller(2).SnappedSize);
        }
    }
}