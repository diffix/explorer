namespace Explorer.Common
{
    using System.Text.Json;

    using Diffix;
    using Explorer.JsonExtensions;

    internal class GroupingSetsResult<T> : ValueWithCount<T>
    {
        protected GroupingSetsResult(int id, string[] labels, DValue<T> value, long count, double? countNoise)
            : base(value, count, countNoise)
        {
            GroupingId = id;
            GroupingLabels = labels;
        }

        public int GroupingId { get; }

        public string[] GroupingLabels { get; }

        public T GroupingValue => Value;

        public string GroupingLabel => GroupingLabels[GroupingIndex];

        public int GroupSize => GroupingLabels.Length;

        public int GroupingIndex =>
            GroupingIdConverter.GetConverter(GroupSize).SingleIndexFromGroupingId(GroupingId);

        public static GroupingSetsResult<T> Create(ref Utf8JsonReader reader, string[] groupingLabels)
        {
            var (groupingId, groupingValue) = reader.ParseGroupingSet<T>(groupingLabels.Length);
            var count = reader.ParseCount();
            var countNoise = reader.ParseNoise();
            return new GroupingSetsResult<T>(groupingId, groupingLabels, groupingValue, count, countNoise);
        }
    }
}