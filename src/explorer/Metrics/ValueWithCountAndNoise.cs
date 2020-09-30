#pragma warning disable CA1815 // ValueWithCount should override Equals

namespace Explorer.Metrics
{
    public struct ValueWithCountAndNoise<T>
    {
        public ValueWithCountAndNoise(T value, long count, double? noise)
        {
            Value = value;
            Count = count;
            Noise = noise;
        }

        public T Value { get; }

        public long Count { get; }

        public double? Noise { get; }
    }
}

#pragma warning restore CA1815 // ValueWithCount should override Equals