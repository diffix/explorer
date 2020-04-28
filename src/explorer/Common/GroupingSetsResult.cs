namespace Explorer.Common
{
    using System.Text.Json;

    internal class GroupingSetsResult<T> : IndexedGroupingSetsResult<string, T>
    {
        internal GroupingSetsResult(ref Utf8JsonReader reader, string[] groupingLabels)
            : base(ref reader, groupingLabels)
        {
        }
    }
}