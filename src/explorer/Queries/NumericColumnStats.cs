namespace Explorer.Queries
{
    using System.Text.Json;

    using Diffix;
    using Explorer.JsonExtensions;

    internal class NumericColumnStats :
        DQuery<NumericColumnStats.Result<long>>,
        DQuery<NumericColumnStats.Result<double>>,
        DQuery<NumericColumnStats.Result<System.DateTime>>
    {
        public NumericColumnStats(string tableName, string columnName)
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

        Result<long> DQuery<Result<long>>.ParseRow(ref Utf8JsonReader reader)
        {
            return new Result<long>
            {
                Min = reader.ParseNonNullableMetric<long>(),
                Max = reader.ParseNonNullableMetric<long>(),
                Count = reader.ParseCount(),
                CountNoise = reader.ParseCountNoise(),
            };
        }

        Result<double> DQuery<Result<double>>.ParseRow(ref Utf8JsonReader reader)
        {
            return new Result<double>
            {
                Min = reader.ParseNonNullableMetric<double>(),
                Max = reader.ParseNonNullableMetric<double>(),
                Count = reader.ParseCount(),
                CountNoise = reader.ParseCountNoise(),
            };
        }

        Result<System.DateTime> DQuery<Result<System.DateTime>>.ParseRow(ref Utf8JsonReader reader)
        {
            return new Result<System.DateTime>
            {
                Min = reader.ParseNonNullableMetric<System.DateTime>(),
                Max = reader.ParseNonNullableMetric<System.DateTime>(),
                Count = reader.ParseCount(),
                CountNoise = reader.ParseCountNoise(),
            };
        }

        public class Result<T>
            where T : unmanaged
        {
            public T Min { get; set; }

            public T Max { get; set; }

            public long Count { get; set; }

            public double? CountNoise { get; set; }
        }
    }
}