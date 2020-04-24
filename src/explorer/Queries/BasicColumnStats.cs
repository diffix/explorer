namespace Explorer.Queries
{
    using System.Text.Json;

    using Diffix;
    using Explorer.JsonExtensions;

    internal class BasicColumnStats<T> :
        DQuery<BasicColumnStats<T>.Result>
    {
        public BasicColumnStats(string tableName, string columnName)
        {
            QueryStatement = $@"
                select
                    min({columnName}),
                    max({columnName}),
                    count(*),
                    count_noise(*)
                from {tableName}";
        }

        public string QueryStatement { get; }

        Result DQuery<Result>.ParseRow(ref Utf8JsonReader reader) => new Result(ref reader);

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