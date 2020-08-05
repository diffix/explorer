namespace Explorer.Queries
{
    using System.Collections.Immutable;
    using System.Linq;
    using System.Text.Json;

    using Diffix;
    using Explorer.Common;

    internal class CyclicalDatetimes :
        DQuery<GroupingSetsResult<int>>
    {
        public static readonly ImmutableArray<string> DateTimeComponents = ImmutableArray.Create(
            "year", "quarter", "month", "day", "weekday", "hour", "minute", "second");

        public static readonly ImmutableArray<string> DateComponents = ImmutableArray.Create(
            "year", "quarter", "month", "day", "weekday");

        public static readonly ImmutableArray<string> TimeComponents = ImmutableArray.Create(
            "hour", "minute", "second");

        public CyclicalDatetimes(DValueType columnType = DValueType.Datetime)
        {
            QueryComponents = columnType switch
            {
                DValueType.Datetime => DateTimeComponents,
                DValueType.Timestamp => TimeComponents,
                DValueType.Date => DateComponents,
                _ => throw new System.ArgumentException($"Expected Datetime, Date or Time, got {columnType}."),
            };
        }

        public ImmutableArray<string> QueryComponents { get; }

        public override GroupingSetsResult<int> ParseRow(ref Utf8JsonReader reader) =>
            new GroupingSetsResult<int>(ref reader, QueryComponents);

        protected override string GetQueryStatement(string table, string column)
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
    }
}