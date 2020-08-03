namespace Explorer.Queries
{
    using System.Text.Json;

    using Diffix;
    using Explorer.JsonExtensions;

    internal class Max :
        DQuery<Max.Result<long>>,
        DQuery<Max.Result<double>>,
        DQuery<Max.Result<decimal>>
    {
        private readonly decimal? lowerBound;

        public Max(decimal? lowerBound = null)
        {
            this.lowerBound = lowerBound;
        }

        public string GetQueryStatement(string table, string column)
        {
            var whereFragment = lowerBound.HasValue ?
                $"where {column} between {lowerBound.Value} and {lowerBound.Value * 2}" :
                string.Empty;

            return $@"
                select
                    max({column})
                from {table}
                {whereFragment}";
        }

        Result<long> DQuery<Result<long>>.ParseRow(ref Utf8JsonReader reader) =>
            new Result<long>
            {
                Max = reader.ParseNullableMetric<long>(),
            };

        Result<double> DQuery<Result<double>>.ParseRow(ref Utf8JsonReader reader) =>
            new Result<double>
            {
                Max = reader.ParseNullableMetric<double>(),
            };

        Result<decimal> DQuery<Result<decimal>>.ParseRow(ref Utf8JsonReader reader) =>
            new Result<decimal>
            {
                Max = reader.ParseNullableMetric<decimal>(),
            };

        public class Result<T>
            where T : struct
        {
            public T? Max { get; set; }
        }
    }
}