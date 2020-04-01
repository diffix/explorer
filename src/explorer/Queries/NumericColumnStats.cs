namespace Explorer.Queries
{
    using System.Text.Json;

    using Diffix;

    internal class NumericColumnStats :
        IQuerySpec<NumericColumnStats.Result<long>>,
        IQuerySpec<NumericColumnStats.Result<double>>,
        IQuerySpec<NumericColumnStats.Result<System.DateTime>>
    {
        public NumericColumnStats(string tableName, string columnName)
        {
            TableName = tableName;
            ColumnName = columnName;
        }

        public string QueryStatement => $@"
                        select
                            min({ColumnName}),
                            max({ColumnName}),
                            count(*),
                            count_noise(*)
                        from {TableName}";

        private string TableName { get; }

        private string ColumnName { get; }

        Result<long> IQuerySpec<Result<long>>.FromJsonArray(ref Utf8JsonReader reader)
        {
            return new Result<long>
            {
                Min = reader.ParseNonNullableMetric<long>(),
                Max = reader.ParseNonNullableMetric<long>(),
                Count = reader.ParseCount(),
                CountNoise = reader.ParseCountNoise(),
            };
        }

        Result<double> IQuerySpec<Result<double>>.FromJsonArray(ref Utf8JsonReader reader)
        {
            return new Result<double>
            {
                Min = reader.ParseNonNullableMetric<double>(),
                Max = reader.ParseNonNullableMetric<double>(),
                Count = reader.ParseCount(),
                CountNoise = reader.ParseCountNoise(),
            };
        }

        Result<System.DateTime> IQuerySpec<Result<System.DateTime>>.FromJsonArray(ref Utf8JsonReader reader)
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