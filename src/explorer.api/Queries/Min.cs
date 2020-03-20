namespace Explorer.Queries
{
    using System.Text.Json;
    using Aircloak.JsonApi;
    using Aircloak.JsonApi.JsonConversion;

    internal class Min :
        IQuerySpec<Min.Result<long>>,
        IQuerySpec<Min.Result<double>>,
        IQuerySpec<Min.Result<decimal>>
    {
        public Min(string tableName, string columnName, decimal? upperBound = null)
        {
            TableName = tableName;
            ColumnName = columnName;
            UpperBound = upperBound;
        }

        public string QueryStatement
        {
            get
            {
                var whereFragment = string.Empty;
                if (UpperBound.HasValue)
                {
                    whereFragment = $"where {ColumnName} between 0 and {UpperBound.Value}";
                }

                return $@"
                    select
                        min({ColumnName})
                    from {TableName}
                    {whereFragment}";
            }
        }

        private string TableName { get; }

        private string ColumnName { get; }

        private decimal? UpperBound { get; }

        Result<long> IQuerySpec<Result<long>>.FromJsonArray(ref Utf8JsonReader reader) =>
            new Result<long>
            {
                Min = reader.ParseNullableMetric<long>(),
            };

        Result<double> IQuerySpec<Result<double>>.FromJsonArray(ref Utf8JsonReader reader) =>
            new Result<double>
            {
                Min = reader.ParseNullableMetric<double>(),
            };

        Result<decimal> IQuerySpec<Result<decimal>>.FromJsonArray(ref Utf8JsonReader reader) =>
            new Result<decimal>
            {
                Min = reader.ParseNullableMetric<decimal>(),
            };

        public class Result<T>
            where T : struct
        {
            public T? Min { get; set; }
        }
    }
}