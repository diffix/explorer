#pragma warning disable CA1815 // ValueWithCount should override Equals

namespace Explorer.Metrics
{
    public struct ValueWithCount<T>
    {
        public ValueWithCount(T value, long count)
        {
            Value = value;
            Count = count;
        }

        public T Value { get; }

        public long Count { get; }
    }
}

#pragma warning restore CA1815 // ValueWithCount should override Equals