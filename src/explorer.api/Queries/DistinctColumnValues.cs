namespace Explorer.Queries
{
    using System.Text.Json;

    using Aircloak.JsonApi;
    using Aircloak.JsonApi.JsonReaderExtensions;
    using Aircloak.JsonApi.ResponseTypes;

    using Explorer.Diffix.Interfaces;

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

        public class Result : ICountAggregate, INullable, ISuppressible
        {
            public Result(ref Utf8JsonReader reader)
            {
                DistinctData = reader.ParseAircloakResultValue<JsonElement>();
                Count = reader.ParseCount();
                CountNoise = reader.ParseCountNoise();
            }

            public AircloakValue<JsonElement> DistinctData { get; set; }

            public long Count { get; set; }

            public double? CountNoise { get; set; }

            public bool IsNull => DistinctData.IsNull;

            public bool IsSuppressed => DistinctData.IsSuppressed;
        }
    }
}