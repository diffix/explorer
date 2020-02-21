namespace Explorer.Queries
{
    using System.Text.Json;

    using Aircloak.JsonApi;
    using Explorer.Api.Models;

    internal class NumericColumnStats :
        IQuerySpec<NumericColumnStats.IntegerResult>,
        IQuerySpec<NumericColumnStats.RealResult>
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

        public string TableName { get; }

        public string ColumnName { get; }

        IntegerResult IQuerySpec<IntegerResult>.FromJsonArray(ref Utf8JsonReader reader)
        {
            reader.Read();
            var min = reader.GetInt64();
            reader.Read();
            var max = reader.GetInt64();
            reader.Read();
            var count = reader.GetInt64();
            reader.Read();
            var countNoise = reader.GetDouble();

            return new IntegerResult
            {
                Min = min,
                Max = max,
                Count = count,
                CountNoise = countNoise,
            };
        }

        RealResult IQuerySpec<RealResult>.FromJsonArray(ref Utf8JsonReader reader)
        {
            reader.Read();
            var min = reader.GetDouble();
            reader.Read();
            var max = reader.GetDouble();
            reader.Read();
            var count = reader.GetInt64();
            reader.Read();
            var countNoise = reader.GetDouble();

            return new RealResult
            {
                Min = min,
                Max = max,
                Count = count,
                CountNoise = countNoise,
            };
        }

        public class IntegerResult
        {
            public long Min { get; set; }

            public long Max { get; set; }

            public long Count { get; set; }

            public double CountNoise { get; set; }
        }

        public class RealResult
        {
            public double Min { get; set; }

            public double Max { get; set; }

            public long Count { get; set; }

            public double CountNoise { get; set; }
        }
    }
}