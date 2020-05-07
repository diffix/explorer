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

        public T Value => DValue.Value;

        public long Count { get; }

        public double? CountNoise { get; }

        public bool IsNull => DValue.IsNull;

        public bool IsSuppressed => DValue.IsSuppressed;

        public bool HasValue => DValue.HasValue;

        protected DValue<T> DValue { get; }
    }
}