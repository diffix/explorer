namespace Explorer.Queries
{
    using System;
    using System.Linq;
    using System.Text.Json;

    using Diffix;
    using Explorer.Common;

    internal class BucketedDatetimes :
        DQuery<GroupingSetsResult<DateTime>>
    {
        public static readonly string[] DateComponents = new[]
        {
            "year",
            "quarter",
            "month",
            "day",
        };

        public static readonly string[] TimeComponents = new[]
        {
            "hour",
            "minute",
            "second",
        };

        public BucketedDatetimes(DValueType columnType = DValueType.Datetime)
        {
            QueryComponents = columnType switch
            {
                DValueType.Datetime => DateComponents.Concat(TimeComponents).ToArray(),
                DValueType.Timestamp => TimeComponents,
                DValueType.Date => DateComponents,
                _ => throw new ArgumentException($"Expected Datetime, Date or Time, got {columnType}."),
            };
        }

        public string[] QueryComponents { get; }

        public string BuildQueryStatement(DSqlObjectName table, DSqlObjectName column)
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

        public GroupingSetsResult<DateTime> ParseRow(ref Utf8JsonReader reader) =>
            new GroupingSetsResult<DateTime>(ref reader, QueryComponents);
    }
}