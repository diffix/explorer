namespace Explorer.Queries
{
    using System.Linq;
    using System.Text.Json;

    using Diffix;
    using Explorer.Common;

    internal class CyclicalDatetimes :
        DQuery<GroupingSetsResult<int>>
    {
        public static readonly string[] DateComponents = new[]
        {
            "year",
            "quarter",
            "month",
            "day",
            "weekday",
        };

        public static readonly string[] TimeComponents = new[]
        {
            "hour",
            "minute",
            "second",
        };

        public CyclicalDatetimes(DValueType columnType = DValueType.Datetime)
        {
            QueryComponents = columnType switch
            {
                DValueType.Datetime => DateComponents.Concat(TimeComponents).ToArray(),
                DValueType.Timestamp => TimeComponents,
                DValueType.Date => DateComponents,
                _ => throw new System.ArgumentException($"Expected Datetime, Date or Time, got {columnType}."),
            };
        }

        public string[] QueryComponents { get; }

        public string BuildQueryStatement(string table, string column)
        {
            var groupsFragment = string.Join(",\n", QueryComponents.Select(s => $"{s}({column})"));
            var groupingSets = string.Join(", ", Enumerable.Range(2, QueryComponents.Length));

            return $@"
                select
                    grouping_id(
                        {groupsFragment}
                    ),
                    {groupsFragment},
                    count(*),
                    count_noise(*)
                from {table}
                group by grouping sets ({groupingSets})";
        }

        public GroupingSetsResult<int> ParseRow(ref Utf8JsonReader reader) =>
            new GroupingSetsResult<int>(ref reader, QueryComponents);
    }
}