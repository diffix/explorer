namespace Explorer.Queries
{
    using System.Linq;
    using System.Text.Json;

    using Aircloak.JsonApi;

    internal class BucketedDatetimes :
        IQuerySpec<BucketedDatetimes.Result>
    {
        public static readonly string[] DateComponents = new[]
        {
            "year",
            "quarter",
            "month",
            "day",
            "hour",
            "minute",
            "second",
        };

        public BucketedDatetimes(
            string tableName,
            string columnName)
        {
            TableName = tableName;
            ColumnName = columnName;
        }

        public string QueryStatement
        {
            get
            {
                var groupsFragment = string.Join(",\n", DateComponents.Select(s => $"date_trunc('{s}', {ColumnName})"));
                var groupingSets = string.Join(", ", Enumerable.Range(2, DateComponents.Length));

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

        private string TableName { get; }

        private string ColumnName { get; }

        public Result FromJsonArray(ref Utf8JsonReader reader) => new Result(ref reader);

        public class Result : GroupingSetsResult<System.DateTime>
        {
            public Result(ref Utf8JsonReader reader)
                : base(ref reader)
            {
            }

            public override string[] GroupingLabels { get => DateComponents; }
        }
    }
}