namespace Explorer.Queries
{
    using System.Text.Json;

    using Diffix;
    using Explorer.JsonExtensions;

    internal class NumericColumnStats :
        DQuery,
        DResultParser<NumericColumnStats.Result<long>>,
        DResultParser<NumericColumnStats.Result<double>>,
        DResultParser<NumericColumnStats.Result<System.DateTime>>
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

        Result<long> DResultParser<Result<long>>.ParseRow(ref Utf8JsonReader reader)
        {
            return new Result<long>
            {
                Min = reader.ParseNonNullableMetric<long>(),
                Max = reader.ParseNonNullableMetric<long>(),
                Count = reader.ParseCount(),
                CountNoise = reader.ParseCountNoise(),
            };
        }

        Result<double> DResultParser<Result<double>>.ParseRow(ref Utf8JsonReader reader)
        {
            return new Result<double>
            {
                Min = reader.ParseNonNullableMetric<double>(),
                Max = reader.ParseNonNullableMetric<double>(),
                Count = reader.ParseCount(),
                CountNoise = reader.ParseCountNoise(),
            };
        }

        Result<System.DateTime> DResultParser<Result<System.DateTime>>.ParseRow(ref Utf8JsonReader reader)
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