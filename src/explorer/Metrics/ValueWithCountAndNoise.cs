#pragma warning disable CA1815 // ValueWithCount should override Equals

namespace Explorer.Metrics
{
    public struct ValueWithCountAndNoise<T>
    {
        public ValueWithCountAndNoise(T value, long count, double? countNoise)
        {
            Value = value;
            Count = count;
            CountNoise = countNoise;
        }

        public T Value { get; }

        public long Count { get; }

        public double? CountNoise { get; }
    }
}

#pragma warning restore CA1815 // ValueWithCount should override Equals