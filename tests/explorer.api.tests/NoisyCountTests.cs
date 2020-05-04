namespace Explorer.Common.Tests
{
    using Explorer.Common;
    using Xunit;

    public sealed class NoisyCountTests
    {
        [Fact]
        public void ZeroIsZero()
        {
            var zero = NoisyCount.Zero;
            Assert.True(zero.Count == 0);
            Assert.True(zero.Variance == 0);
            Assert.True(zero.Noise == 0);
            Assert.True(zero == NoisyCount.Zero);
        }

        [Theory]
        [InlineData(100, 1.4)]
        [InlineData(40, 4.3)]
        public void AddingZeroDoesNothing(long count, double noise)
        {
            var nc = NoisyCount.WithNoise(count, noise);
            var result = NoisyCount.Zero + nc;
            Assert.True(result == nc);
        }

        [Fact]
        public void NoiseValuesCombinedCorrectly()
        {
            var a = NoisyCount.WithNoise(100, 3);
            var b = NoisyCount.WithNoise(200, 4);
            var expected = NoisyCount.WithNoise(300, 5);

            var result = a + b;
            Assert.True(result == expected);
        }
    }
}