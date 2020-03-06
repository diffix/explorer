namespace Explorer.Queries
{
    using System.Text.Json;

    using Aircloak.JsonApi;
    using Aircloak.JsonApi.ResponseTypes;
    using Aircloak.JsonApi.JsonReaderExtensions;

    internal class DistinctColumnValues :
        IQuerySpec<DistinctColumnValues.Result<long>>,
        IQuerySpec<DistinctColumnValues.Result<double>>,
        IQuerySpec<DistinctColumnValues.Result<bool>>,
        IQuerySpec<DistinctColumnValues.Result<string>>
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

        Result<long> IQuerySpec<Result<long>>.FromJsonArray(ref Utf8JsonReader reader)
        {
            return FromJsonArray<long>(ref reader);
        }

        Result<double> IQuerySpec<Result<double>>.FromJsonArray(ref Utf8JsonReader reader)
        {
            return FromJsonArray<double>(ref reader);
        }

        Result<bool> IQuerySpec<Result<bool>>.FromJsonArray(ref Utf8JsonReader reader)
        {
            return FromJsonArray<bool>(ref reader);
        }

        Result<string> IQuerySpec<Result<string>>.FromJsonArray(ref Utf8JsonReader reader)
        {
            return FromJsonArray<string>(ref reader);
        }

        private Result<T> FromJsonArray<T>(
            ref Utf8JsonReader reader)
        {
            return new Result<T>
            {
                DistinctData = reader.ParseAircloakResultValue<T>(),
                Count = reader.ParseCount(),
                CountNoise = reader.ParseCountNoise(),
            };
        }

        public class Result<T>
        {
            public AircloakValue<T> DistinctData { get; set; } = NullValue<T>.Instance;

            public long Count { get; set; }

            public double? CountNoise { get; set; }
        }
    }
}