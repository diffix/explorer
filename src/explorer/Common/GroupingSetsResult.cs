namespace Explorer.Common
{
    using System.Text.Json;

    using Diffix;
    using Explorer.JsonExtensions;

    internal class GroupingSetsResult<T> : IndexedGroupingSetsResult<string, T>
    {
        protected GroupingSetsResult(int id, string[] labels, DValue<T> value, long count, double? countNoise)
            : base(id, labels, value, count, countNoise)
        {
        }

        public static new GroupingSetsResult<T> Create(ref Utf8JsonReader reader, string[] groupingLabels)
        {
            var (groupingId, groupingValue) = reader.ParseGroupingSet<T>(groupingLabels.Length);
            var count = reader.ParseCount();
            var countNoise = reader.ParseNoise();
            return new GroupingSetsResult<T>(
                groupingId, groupingLabels, groupingValue, count, countNoise);
        }
    }
}