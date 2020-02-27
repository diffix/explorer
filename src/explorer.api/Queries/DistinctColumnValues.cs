namespace Explorer.Queries
{
    using System.Text.Json;

    using Aircloak.JsonApi;
    using Aircloak.JsonApi.ResponseTypes;

    internal class DistinctColumnValues :
        IQuerySpec<DistinctColumnValues.IntegerResult>,
        IQuerySpec<DistinctColumnValues.RealResult>,
        IQuerySpec<DistinctColumnValues.BoolResult>,
        IQuerySpec<DistinctColumnValues.TextResult>
    {
        public DistinctColumnValues(string tableName, string columnName)
        {
            TableName = tableName;
            ColumnName = columnName;
        }

        public string QueryStatement => $@"
                        select
                            {ColumnName},
                            count(*),
                            count_noise(*)
                        from {TableName}
                        group by {ColumnName}";

        private string TableName { get; }

        private string ColumnName { get; }

        IntegerResult IQuerySpec<IntegerResult>.FromJsonArray(ref Utf8JsonReader reader)
        {
            reader.Read();
            var columnValue = AircloakColumnJsonParser.ParseLong(ref reader);
            var (count, countNoise) = ReadCountAndNoise(ref reader);

            return new IntegerResult
            {
                ColumnValue = columnValue,
                Count = count,
                CountNoise = countNoise,
            };
        }

        RealResult IQuerySpec<RealResult>.FromJsonArray(ref Utf8JsonReader reader)
        {
            reader.Read();
            var columnValue = AircloakColumnJsonParser.ParseDouble(ref reader);
            var (count, countNoise) = ReadCountAndNoise(ref reader);

            return new RealResult
            {
                ColumnValue = columnValue,
                Count = count,
                CountNoise = countNoise,
            };
        }

        BoolResult IQuerySpec<BoolResult>.FromJsonArray(ref Utf8JsonReader reader)
        {
            reader.Read();
            var columnValue = AircloakColumnJsonParser.ParseBool(ref reader);
            var (count, countNoise) = ReadCountAndNoise(ref reader);

            return new BoolResult
            {
                ColumnValue = columnValue,
                Count = count,
                CountNoise = countNoise,
            };
        }

        TextResult IQuerySpec<TextResult>.FromJsonArray(ref Utf8JsonReader reader)
        {
            reader.Read();
            var columnValue = AircloakColumnJsonParser.ParseString(ref reader);
            var (count, countNoise) = ReadCountAndNoise(ref reader);

            return new TextResult
            {
                ColumnValue = columnValue,
                Count = count,
                CountNoise = countNoise,
            };
        }

        private (long, double?) ReadCountAndNoise(ref Utf8JsonReader reader)
        {
            reader.Read();
            var count = reader.GetInt64();
            reader.Read();
            double? countNoise = null;
            if (reader.TokenType != JsonTokenType.Null)
            {
                countNoise = reader.GetDouble();
            }

            return (count, countNoise);
        }

        public class IntegerResult
        {
            public IntegerResult()
            {
                ColumnValue = new NullColumn<long>();
            }

            public AircloakColumn<long> ColumnValue { get; set; }

            public long Count { get; set; }

            public double? CountNoise { get; set; }
        }

        public class RealResult
        {
            public RealResult()
            {
                ColumnValue = new NullColumn<double>();
            }

            public AircloakColumn<double> ColumnValue { get; set; }

            public long Count { get; set; }

            public double? CountNoise { get; set; }
        }

        public class BoolResult
        {
            public BoolResult()
            {
                ColumnValue = new NullColumn<bool>();
            }

            public AircloakColumn<bool> ColumnValue { get; set; }

            public long Count { get; set; }

            public double? CountNoise { get; set; }
        }

        public class TextResult
        {
            public TextResult()
            {
                ColumnValue = new NullColumn<string>();
            }

            public AircloakColumn<string> ColumnValue { get; set; }

            public long Count { get; set; }

            public double? CountNoise { get; set; }
        }
    }
}