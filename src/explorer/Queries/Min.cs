namespace Explorer.Queries
{
    using System.Text.Json;

    using Diffix;
    using Explorer.JsonExtensions;

    internal class Min :
        DQuery<Min.Result<long>>,
        DQuery<Min.Result<double>>,
        DQuery<Min.Result<decimal>>
    {
        public Min(DSqlObjectName tableName, DSqlObjectName columnName, decimal? upperBound = null)
        {
            var whereFragment = string.Empty;
            if (upperBound.HasValue)
            {
                whereFragment = $"where {columnName} between 0 and {upperBound.Value}";
            }

            QueryStatement = $@"
                select
                    min({columnName})
                from {tableName}
                {whereFragment}";
        }

        public string QueryStatement { get; }

        Result<long> DQuery<Result<long>>.ParseRow(ref Utf8JsonReader reader) =>
            new Result<long>
            {
                Min = reader.ParseNullableMetric<long>(),
            };

        Result<double> DQuery<Result<double>>.ParseRow(ref Utf8JsonReader reader) =>
            new Result<double>
            {
                Min = reader.ParseNullableMetric<double>(),
            };

        Result<decimal> DQuery<Result<decimal>>.ParseRow(ref Utf8JsonReader reader) =>
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