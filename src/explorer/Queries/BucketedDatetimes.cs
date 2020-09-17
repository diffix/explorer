namespace Explorer.Queries
{
    using System;
    using System.Collections.Immutable;
    using System.Linq;
    using System.Text.Json;

    using Diffix;
    using Explorer.Common.Utils;

    internal class BucketedDatetimes :
        DQuery<GroupingSetsResult<DateTime>>
    {
        public static readonly ImmutableArray<string> DateTimeComponents = ImmutableArray.Create(
            "year", "quarter", "month", "day", "hour", "minute", "second");

        public static readonly ImmutableArray<string> DateComponents = ImmutableArray.Create(
            "year", "quarter", "month", "day");

        public static readonly ImmutableArray<string> TimeComponents = ImmutableArray.Create(
            "hour", "minute", "second");

        public BucketedDatetimes(DValueType columnType = DValueType.Datetime)
        {
            QueryComponents = columnType switch
            {
                DValueType.Datetime => DateTimeComponents,
                DValueType.Timestamp => TimeComponents,
                DValueType.Date => DateComponents,
                _ => throw new ArgumentException($"Expected Datetime, Date or Time, got {columnType}."),
            };
        }

        public ImmutableArray<string> QueryComponents { get; }

        public override GroupingSetsResult<DateTime> ParseRow(ref Utf8JsonReader reader) =>
            new GroupingSetsResult<DateTime>(ref reader, QueryComponents);

        protected override string GetQueryStatement(string table, string column)
        {
            var groupsFragment = string.Join(",\n", QueryComponents.Select(s => $"date_trunc('{s}', {column})"));
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