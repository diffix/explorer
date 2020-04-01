namespace Diffix
{
    using Diffix.Utils;

    public interface IGroupingSetsAggregate
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

    public interface IGroupingSetsAggregate<T> : IGroupingSetsAggregate, IDiffixValue<T>
    {
        public IDiffixValue<T> GroupingValue { get; }
    }
}