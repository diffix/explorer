namespace Explorer.Queries
{
    using System.Text.Json;

    using Aircloak.JsonApi.JsonConversion;
    using Aircloak.JsonApi.ResponseTypes;

    using Explorer.Diffix.Extensions;
    using Explorer.Diffix.Interfaces;

    internal abstract class GroupingSetsResult<T> : ICountAggregate, ISuppressible, IGroupingSetsAggregate<T>
    {
        protected GroupingSetsResult(ref Utf8JsonReader reader, int groupSize)
        {
            (GroupingId, GroupingValue) = reader.ParseGroupingSet<T>(groupSize);
            Count = reader.ParseCount();
            CountNoise = reader.ParseNoise();
        }

        public int GroupingId { get; }

        public abstract string[] GroupingLabels { get; }

        public AircloakValue<T> GroupingValue { get; }

        public long Count { get; }

        public double? CountNoise { get; }

        public bool IsSuppressed => GroupingValue.IsSuppressed;
    }
}