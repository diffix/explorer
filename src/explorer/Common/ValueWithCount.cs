namespace Explorer.Common
{
    using System.Text.Json;

    using Diffix;
    using Explorer.JsonExtensions;

    internal class ValueWithCount<T> : CountableRow
    {
        public ValueWithCount(DValue<T> value, long count, double? countNoise)
        {
            DValue = value;
            Count = count;
            CountNoise = countNoise;
        }

        public ValueWithCount(ref Utf8JsonReader reader)
        {
            DValue = reader.ParseDValue<T>();
            Count = reader.ParseCount();
            CountNoise = reader.ParseCountNoise();
        }

#pragma warning disable CS8618 // Non-nullable property 'DValue' is uninitialized.
        protected ValueWithCount()
        {
        }
#pragma warning restore CS8618 // Non-nullable property 'DValue' is uninitialized.

        public T Value => DValue.Value;

        public long Count { get; protected set; }

        public double? CountNoise { get; protected set; }

        public bool IsNull => DValue.IsNull;

        public bool IsSuppressed => DValue.IsSuppressed;

        public bool HasValue => DValue.HasValue;

        protected DValue<T> DValue { get; set; }

        public static ValueWithCount<T> Parse(ref Utf8JsonReader reader)
        {
            var value = reader.ParseDValue<T>();
            var count = reader.ParseCount();
            var countNoise = reader.ParseCountNoise();
            return new ValueWithCount<T>(value, count, countNoise);
        }
    }
}