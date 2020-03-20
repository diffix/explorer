namespace Explorer.Queries
{
    using System.Text.Json;

    using Aircloak.JsonApi.JsonReaderExtensions;
    using Aircloak.JsonApi.ResponseTypes;

    using Explorer.Diffix.Extensions;
    using Explorer.Diffix.Interfaces;

    internal abstract class GroupingSetsResult<T> : ICountAggregate, IGroupingSetsAggregate<T>
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
    }
}