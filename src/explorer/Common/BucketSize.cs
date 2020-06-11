namespace Explorer.Common
{
    using System.Linq;
    using System.Text.Json.Serialization;

    using Explorer.Common.JsonConversion;

    [JsonConverter(typeof(BucketSizeConverter))]
    public class BucketSize
    {
#pragma warning disable SA1137 // Elements should have the same indentation
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
#pragma warning restore SA1137 // Elements should have the same indentation

        private readonly int index;

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
            (SnappedSize, index) = Snap(size);
        }

        public decimal SnappedSize { get; }

        public BucketSize Larger(int steps)
        {
            if (index + steps >= ValidSizes.Length)
            {
                return new BucketSize(ValidSizes.Last());
            }
            return new BucketSize(ValidSizes[index + steps]);
        }

        public BucketSize Smaller(int steps)
        {
            if (index - steps < 0)
            {
                return new BucketSize(ValidSizes[0]);
            }
            return new BucketSize(ValidSizes[index - steps]);
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

        private static (decimal BucketSize, int Index) Snap(decimal size)
        {
            if (size <= 0)
            {
                throw new System.ArgumentException($"Invalid bucket size {size}");
            }
            if (size <= ValidSizes[0])
            {
                return (ValidSizes[0], 0);
            }
            if (size >= ValidSizes.Last())
            {
                return (ValidSizes.Last(), ValidSizes.Length - 1);
            }
            for (var i = 1; i < ValidSizes.Length; i++)
            {
                if (ValidSizes[i] >= size)
                {
                    var lower_diff = size - ValidSizes[i - 1];
                    var upper_diff = ValidSizes[i] - size;

                    return lower_diff < upper_diff ? (ValidSizes[i - 1], i - 1) : (ValidSizes[i], i);
                }
            }
            throw new System.Exception("Failed to find a valid bucket size - This should never happen!");
        }
    }
}