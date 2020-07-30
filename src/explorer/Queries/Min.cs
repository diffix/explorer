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
        private readonly decimal? upperBound;

        public Min(decimal? upperBound = null)
        {
            this.upperBound = upperBound;
        }

        public string BuildQueryStatement(DSqlObjectName table, DSqlObjectName column)
        {
            var whereFragment = upperBound.HasValue ?
                $"where {column} between 0 and {upperBound.Value}" :
                string.Empty;

            return $@"
                select
                    min({column})
                from {table}
                {whereFragment}";
        }

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