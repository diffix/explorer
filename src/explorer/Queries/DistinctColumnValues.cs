namespace Explorer.Queries
{
    using System.Text.Json;

    using Diffix;
    using Explorer.Common;

    internal class DistinctColumnValues :
        IQuerySpec<DistinctColumnValues.Result>
    {
        public DistinctColumnValues(string tableName, string columnName)
        {
            TableName = tableName;
            ColumnName = columnName;
        }

        public string QueryStatement => $@"
                        select
                            {ColumnName},
                            count(*),
                            count_noise(*)
                        from {TableName}
                        group by {ColumnName}";

        private string TableName { get; }

        private string ColumnName { get; }

        public Result FromJsonArray(ref Utf8JsonReader reader) => new Result(ref reader);

        public class Result : ICountAggregate, IDiffixValue
        {
            public Result(ref Utf8JsonReader reader)
            {
                DistinctData = reader.ParseAircloakResultValue<JsonElement>();
                Count = reader.ParseCount();
                CountNoise = reader.ParseCountNoise();
            }

            public IDiffixValue<JsonElement> DistinctData { get; set; }

            public long Count { get; set; }

            public double? CountNoise { get; set; }

            public bool IsNull => DistinctData.IsNull;

            public bool IsSuppressed => DistinctData.IsSuppressed;
        }
    }
}