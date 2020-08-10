namespace Explorer.Common
{
    using System;

    public static class RandomExtensions
    {
        /// <summary>
        /// Returns a random long from min (inclusive) to max (exclusive).
        /// </summary>
        /// <param name="random">The given random instance.</param>
        /// <param name="min">The inclusive minimum bound.</param>
        /// <param name="max">The exclusive maximum bound.  Must be greater than min.</param>
        /// <returns>A 64-bit signed integer that is greater than or equal to min and less than max.</returns>
        public static long NextLong(this Random random, long min, long max)
        {
            if (max <= min)
            {
                throw new ArgumentOutOfRangeException(nameof(max), $"max must be > min! Got max {max} and min {min}");
            }

            // Working with ulong so that modulo works correctly with values > long.MaxValue
            var ulrange = (ulong)(max - min);
            var ulmax = ulong.MaxValue - (((ulong.MaxValue % ulrange) + 1) % ulrange);

            // Prevent a modolo bias; see https://stackoverflow.com/a/10984975/238419
            // for more information.
            // In the worst case, the expected number of calls is 2 (though usually it's
            // much closer to 1) so this loop doesn't really hurt performance at all.
            var buf = new byte[8];
            ulong ulrand;
            do
            {
                random.NextBytes(buf);
                ulrand = (ulong)BitConverter.ToInt64(buf, 0);
            }
            while (ulrand > ulmax);

            return (long)(ulrand % ulrange) + min;
        }

        /// <summary>
        /// Returns a random long from 0 (inclusive) to max (exclusive).
        /// </summary>
        /// <param name="random">The given random instance.</param>
        /// <param name="max">The exclusive maximum bound.  Must be greater than 0.</param>
        /// <returns>A 64-bit signed integer that is greater than or equal to 0 and less than max.</returns>
        public static long NextLong(this Random random, long max)
        {
            return random.NextLong(0, max);
        }
    }
}