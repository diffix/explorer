#pragma warning disable SA1137 // Elements should have the same indentation

namespace Explorer.Queries
{
    using System.Linq;

    public class BucketSize
    {
        private static readonly decimal[] ValidSizes =
        {
                     0.0001M,                0.0002M,                0.0005M,
                      0.001M,                 0.002M,                 0.005M,
                       0.01M,                  0.02M,                  0.05M,
                        0.1M,                   0.2M,                   0.5M,
                        1.0M,                   2.0M,                   5.0M,
                       10.0M,                  20.0M,                  50.0M,
                      100.0M,                 200.0M,                 500.0M,
                    1_000.0M,               2_000.0M,               5_000.0M,
                   10_000.0M,              20_000.0M,              50_000.0M,
                  100_000.0M,             200_000.0M,             500_000.0M,
                1_000_000.0M,           2_000_000.0M,           5_000_000.0M,
               10_000_000.0M,          20_000_000.0M,          50_000_000.0M,
              100_000_000.0M,         200_000_000.0M,         500_000_000.0M,
            1_000_000_000.0M,       2_000_000_000.0M,       5_000_000_000.0M,
           10_000_000_000.0M,      20_000_000_000.0M,      50_000_000_000.0M,
          100_000_000_000.0M,     200_000_000_000.0M,     500_000_000_000.0M,
        1_000_000_000_000.0M,   2_000_000_000_000.0M,   5_000_000_000_000.0M,
        };

        public BucketSize(long size)
            : this((decimal)size)
        {
        }

        public BucketSize(double size)
            : this((decimal)size)
        {
        }

        public BucketSize(decimal size)
        {
            SnappedSize = Snap(size);
        }

        public decimal SnappedSize { get; }

        public static decimal Snap(decimal size)
        {
            if (size <= 0)
            {
                throw new System.ArgumentException($"Invalid bucket size {size}");
            }

            if (size < ValidSizes[0])
            {
                return ValidSizes[0];
            }

            if (size > ValidSizes.Last())
            {
                return ValidSizes.Last();
            }

            if (ValidSizes.Contains(size))
            {
                return size;
            }

            var (below, above) = ValidSizes.Zip(ValidSizes.Skip(1)).First(tup =>
                size >= tup.First && size <= tup.Second);

            var lower_diff = size - below;
            var upper_diff = above - size;

            return lower_diff < upper_diff ? below : above;
        }

        public BucketSize Larger(int steps)
        {
            return new BucketSize(
                ValidSizes
                    .SkipWhile(size => size < SnappedSize)
                    .ElementAt(steps));
        }

        public BucketSize Smaller(int steps)
        {
            return new BucketSize(
                ValidSizes
                    .TakeWhile(size => size <= SnappedSize)
                    .Reverse()
                    .ElementAt(steps));
        }

        public override bool Equals(object? obj)
        {
            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }

            return SnappedSize == ((BucketSize)obj).SnappedSize;
        }

        public override int GetHashCode() => SnappedSize.GetHashCode();
    }
}

#pragma warning restore SA1137 // Elements should have the same indentation