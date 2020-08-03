namespace Explorer.Queries
{
    using System.Text.Json;

    using Diffix;
    using Explorer.JsonExtensions;

    internal class Min :
        DQuery,
        DResultParser<Min.Result<long>>,
        DResultParser<Min.Result<double>>,
        DResultParser<Min.Result<decimal>>
    {
        private readonly decimal? upperBound;

        public Min(decimal? upperBound = null)
        {
            this.upperBound = upperBound;
        }

        Result<long> DResultParser<Result<long>>.ParseRow(ref Utf8JsonReader reader) =>
            new Result<long>
            {
                Min = reader.ParseNullableMetric<long>(),
            };

        Result<double> DResultParser<Result<double>>.ParseRow(ref Utf8JsonReader reader) =>
            new Result<double>
            {
                Min = reader.ParseNullableMetric<double>(),
            };

        Result<decimal> DResultParser<Result<decimal>>.ParseRow(ref Utf8JsonReader reader) =>
            new Result<decimal>
            {
                Min = reader.ParseNullableMetric<decimal>(),
            };

        protected override string GetQueryStatement(string table, string column)
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

        public class Result<T>
            where T : struct
        {
            public T? Min { get; set; }
        }
    }
}