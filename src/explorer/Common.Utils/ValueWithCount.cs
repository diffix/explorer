namespace Explorer.Common
{
    using System.Text.Json;

    using Diffix;
    using Explorer.JsonExtensions;

    public class ValueWithCount<T> : CountableRow
    {
        internal ValueWithCount(ref Utf8JsonReader reader)
        {
            DValue = reader.ParseDValue<T>();
            Count = reader.ParseCount();
            CountNoise = reader.ParseCountNoise();
        }

        private ValueWithCount(DValue<T> dvalue, long count, double? countNoise)
        {
            DValue = dvalue;
            Count = count;
            CountNoise = countNoise;
        }

        public T Value => DValue.Value;

        public long Count { get; }

        public double? CountNoise { get; }

        public bool IsNull => DValue.IsNull;

        public bool IsSuppressed => DValue.IsSuppressed;

        public bool HasValue => DValue.HasValue;

        protected DValue<T> DValue { get; }

#pragma warning disable CA1000 // do not declare static members on generic types
        public static ValueWithCount<T> ValueCount(T value, long count, double? countNoise = null) =>
            new ValueWithCount<T>(DValue<T>.Create(value), count, countNoise);
#pragma warning restore CA1000 // do not declare static members on generic types
    }
}
