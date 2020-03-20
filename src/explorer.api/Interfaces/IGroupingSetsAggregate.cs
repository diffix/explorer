namespace Explorer.Diffix.Interfaces
{
    using Aircloak.JsonApi.ResponseTypes;
    using Explorer.Queries;

    internal interface IGroupingSetsAggregate
    {
        public string[] GroupingLabels { get; }

        public int GroupingId { get; }

        public int GroupSize { get => GroupingLabels.Length; }

        public int GroupingIndex
        {
            get => GroupingIdConverter.GetConverter(GroupSize).SingleIndexFromGroupingId(GroupingId);
        }

        public string GroupingLabel { get => GroupingLabels[GroupingIndex]; }
    }

    internal interface IGroupingSetsAggregate<T> : IGroupingSetsAggregate
    {
        public AircloakValue<T> GroupingValue { get; }
    }
}