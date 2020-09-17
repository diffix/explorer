namespace Explorer.Common
{
    using System.Collections.Immutable;
    using System.Text.Json;

    public class GroupingSetsResult<T> : IndexedGroupingSetsResult<string, T>
    {
        internal GroupingSetsResult(ref Utf8JsonReader reader, ImmutableArray<string> groupingLabels)
            : base(ref reader, groupingLabels)
        {
        }
    }
}