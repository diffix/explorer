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
            var columnValue = AircloakValueJsonParser.ParseLong(ref reader);
            var (count, countNoise) = ReadCountAndNoise(ref reader);

            return new IntegerResult
            {
                DistinctData = columnValue,
                Count = count,
                CountNoise = countNoise,
            };
        }

        RealResult IQuerySpec<RealResult>.FromJsonArray(ref Utf8JsonReader reader)
        {
            reader.Read();
            var columnValue = AircloakValueJsonParser.ParseDouble(ref reader);
            var (count, countNoise) = ReadCountAndNoise(ref reader);

            return new RealResult
            {
                DistinctData = columnValue,
                Count = count,
                CountNoise = countNoise,
            };
        }

        BoolResult IQuerySpec<BoolResult>.FromJsonArray(ref Utf8JsonReader reader)
        {
            reader.Read();
            var columnValue = AircloakValueJsonParser.ParseBool(ref reader);
            var (count, countNoise) = ReadCountAndNoise(ref reader);

            return new BoolResult
            {
                DistinctData = columnValue,
                Count = count,
                CountNoise = countNoise,
            };
        }

        TextResult IQuerySpec<TextResult>.FromJsonArray(ref Utf8JsonReader reader)
        {
            reader.Read();
            var columnValue = AircloakValueJsonParser.ParseString(ref reader);
            var (count, countNoise) = ReadCountAndNoise(ref reader);

            return new TextResult
            {
                DistinctData = columnValue,
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
            public AircloakValue<long> DistinctData { get; set; } = NullValue<long>.Instance;

            public long Count { get; set; }

            public double? CountNoise { get; set; }
        }

        public class RealResult
        {
            public AircloakValue<double> DistinctData { get; set; } = NullValue<double>.Instance;

            public long Count { get; set; }

            public double? CountNoise { get; set; }
        }

        public class BoolResult
        {
            public AircloakValue<bool> DistinctData { get; set; } = NullValue<bool>.Instance;

            public long Count { get; set; }

            public double? CountNoise { get; set; }
        }

        public class TextResult
        {
            public AircloakValue<string> DistinctData { get; set; } = NullValue<string>.Instance;

            public long Count { get; set; }

            public double? CountNoise { get; set; }
        }
    }
}