namespace Explorer.Queries
{
    using System.Linq;
    using System.Text.Json;

    using Aircloak.JsonApi;
    using Aircloak.JsonApi.ResponseTypes;

    internal class BucketedDatetimes :
        IQuerySpec<BucketedDatetimes.Result>
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

        public BucketedDatetimes(
            string tableName,
            string columnName,
            AircloakType columnType = AircloakType.Datetime)
        {
            TableName = tableName;
            ColumnName = columnName;
            QueryComponents = columnType switch
            {
                AircloakType.Datetime => DateComponents.Concat(TimeComponents).ToArray(),
                AircloakType.Timestamp => TimeComponents,
                AircloakType.Date => DateComponents,
                _ => throw new System.ArgumentException($"Expected Datetime, Date or Time, got {columnType}."),
            };
        }

        public string QueryStatement
        {
            get
            {
                var groupsFragment = string.Join(",\n", QueryComponents.Select(s => $"date_trunc('{s}', {ColumnName})"));
                var groupingSets = string.Join(", ", Enumerable.Range(2, QueryComponents.Length));

                return $@"
                select
                    grouping_id(
                        {groupsFragment}
                    ),
                    {groupsFragment},
                    count(*),
                    count_noise(*)
                from {TableName}
                group by grouping sets ({groupingSets})
                ";
            }
        }

        public string[] QueryComponents { get; }

        private string TableName { get; }

        private string ColumnName { get; }

        public Result FromJsonArray(ref Utf8JsonReader reader) => new Result(ref reader, QueryComponents);

        public class Result : GroupingSetsResult<System.DateTime>
        {
            public Result(ref Utf8JsonReader reader, string[] groupingLabels)
                : base(ref reader, groupingLabels.Length)
            {
                GroupingLabels = groupingLabels;
            }

            public override string[] GroupingLabels { get; }
        }
    }
}