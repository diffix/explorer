namespace Explorer.Metrics
{
    public class ValueWithCount<T>
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