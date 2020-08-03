namespace Explorer.Queries
{
    using System.Text.Json;

    using Diffix;
    using Explorer.JsonExtensions;

    public class BasicColumnStats<T> :
        DQuery,
        DResultParser<BasicColumnStats<T>.Result>
    {
        protected override string GetQueryStatement(string table, string column)
        {
            return $@"
                select
                    min({column}),
                    max({column}),
                    count(*),
                    count_noise(*)
                from {table}";
        }

        Result DResultParser<Result>.ParseRow(ref Utf8JsonReader reader) => new Result(ref reader);

        public class Result
        {
            public Result(ref Utf8JsonReader reader)
            {
                Min = reader.ParseNonNullableMetric<T>();
                Max = reader.ParseNonNullableMetric<T>();
                Count = reader.ParseCount();
                CountNoise = reader.ParseCountNoise();
            }

            public T Min { get; set; }

            public T Max { get; set; }

            public long Count { get; set; }

            public double? CountNoise { get; set; }
        }
    }
}