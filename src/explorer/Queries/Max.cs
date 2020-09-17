namespace Explorer.Queries
{
    using System.Text.Json;

    using Diffix;
    using Explorer.Common.JsonConversion;

    internal class Max :
        DQueryStatement,
        DResultParser<Max.Result<long>>,
        DResultParser<Max.Result<double>>,
        DResultParser<Max.Result<decimal>>
    {
        private readonly decimal? lowerBound;

        public Max(decimal? lowerBound = null)
        {
            this.lowerBound = lowerBound;
        }

        Result<long> DResultParser<Result<long>>.ParseRow(ref Utf8JsonReader reader) =>
            new Result<long>
            {
                Max = reader.ParseNullableMetric<long>(),
            };

        Result<double> DResultParser<Result<double>>.ParseRow(ref Utf8JsonReader reader) =>
            new Result<double>
            {
                Max = reader.ParseNullableMetric<double>(),
            };

        Result<decimal> DResultParser<Result<decimal>>.ParseRow(ref Utf8JsonReader reader) =>
            new Result<decimal>
            {
                Max = reader.ParseNullableMetric<decimal>(),
            };

        protected override string GetQueryStatement(string table, string column)
        {
            var whereFragment = lowerBound.HasValue
                ? lowerBound >= 0
                    ? $"where {column} between {lowerBound.Value} and {lowerBound.Value * 2}"
                    : $"where {column} between {lowerBound.Value} and 0"
                : string.Empty;

            return $@"
                select
                    max({column})
                from {table}
                {whereFragment}";
        }

        public class Result<T>
            where T : struct
        {
            public T? Max { get; set; }
        }
    }
}