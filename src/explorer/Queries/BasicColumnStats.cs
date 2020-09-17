namespace Explorer.Queries
{
    using System.Text.Json;

    using Diffix;
    using Explorer.Common.JsonConversion;

    public class BasicColumnStats<T> :
        DQuery<BasicColumnStats<T>.Result>
        where T : unmanaged
    {
        public override Result ParseRow(ref Utf8JsonReader reader) => new Result(ref reader);

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

        public class Result
        {
            public Result(ref Utf8JsonReader reader)
            {
                Min = reader.ParseNullableMetric<T>();
                Max = reader.ParseNullableMetric<T>();
                Count = reader.ParseCount();
                CountNoise = reader.ParseCountNoise();
            }

            public T? Min { get; set; }

            public T? Max { get; set; }

            public long Count { get; set; }

            public double? CountNoise { get; set; }
        }
    }
}