namespace Explorer.Common
{
    using System.Text.Json;

    using Diffix;
    using Explorer.JsonExtensions;

    internal class ValueWithCount<T>
    {
        public ValueWithCount(DValue<T> value, long count, double? countNoise)
        {
            DValue = value;
            Count = count;
            CountNoise = countNoise;
        }

        public T Value => DValue.Value;

        public long Count { get; }

        public double? CountNoise { get; }

        public bool IsNull => DValue.IsNull;

        public bool IsSuppressed => DValue.IsSuppressed;

        public bool HasValue => DValue.HasValue;

        private DValue<T> DValue { get; }

        public static ValueWithCount<T> Parse(ref Utf8JsonReader reader)
        {
            var value = reader.ParseValue<T>();
            var count = reader.ParseCount();
            var countNoise = reader.ParseCountNoise();
            return new ValueWithCount<T>(value, count, countNoise);
        }
    }
}