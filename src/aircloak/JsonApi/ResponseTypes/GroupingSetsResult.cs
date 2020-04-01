namespace Aircloak.JsonApi.ResponseTypes
{
    using System.Text.Json;

    using Aircloak.JsonApi.JsonConversion;
    using Diffix;

    internal abstract class GroupingSetsResult<T> : IDiffixValue<T>, IGroupingSetsAggregate<T>
    {
        protected GroupingSetsResult(ref Utf8JsonReader reader, int groupSize)
        {
            (GroupingId, GroupingValue) = reader.ParseGroupingSet<T>(groupSize);
            Count = reader.ParseCount();
            CountNoise = reader.ParseNoise();
        }

        public int GroupingId { get; }

        public abstract string[] GroupingLabels { get; }

        public IDiffixValue<T> GroupingValue { get; }

        public long Count { get; }

        public double? CountNoise { get; }

        public bool IsSuppressed => GroupingValue.IsSuppressed;

        public bool IsNull => GroupingValue.IsNull;

        public bool HasValue => GroupingValue.HasValue;

        T IDiffixValue<T>.Value => GroupingValue.Value;
    }
}