using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("explorer.api.tests")]

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

        public string TableName { get; }

        public string ColumnName { get; }

        public class IntegerResult : IJsonArrayConvertible
        {
            public IntegerResult()
            {
                ColumnValue = new NullColumn<long>();
            }

            public AircloakColumn<long> ColumnValue { get; set; }

            public long? Count { get; set; }

            public double? CountNoise { get; set; }

            void IJsonArrayConvertible.FromArrayValues(ref Utf8JsonReader reader)
            {
                reader.Read();
                ColumnValue = AircloakColumnJsonParser.ParseLong(ref reader);
                reader.Read();
                Count = reader.GetInt64();
                reader.Read();
                if (reader.TokenType != JsonTokenType.Null)
                {
                    CountNoise = reader.GetDouble();
                }
            }
        }

        public class RealResult : IJsonArrayConvertible
        {
            public RealResult()
            {
                ColumnValue = new NullColumn<double>();
            }

            public AircloakColumn<double> ColumnValue { get; set; }

            public long? Count { get; set; }

            public double? CountNoise { get; set; }

            void IJsonArrayConvertible.FromArrayValues(ref Utf8JsonReader reader)
            {
                reader.Read();
                ColumnValue = AircloakColumnJsonParser.ParseDouble(ref reader);
                reader.Read();
                Count = reader.GetInt64();
                reader.Read();
                if (reader.TokenType != JsonTokenType.Null)
                {
                    CountNoise = reader.GetDouble();
                }
            }
        }

        public class BoolResult : IJsonArrayConvertible
        {
            public BoolResult()
            {
                ColumnValue = new NullColumn<bool>();
            }

            public AircloakColumn<bool> ColumnValue { get; set; }

            public long? Count { get; set; }

            public double? CountNoise { get; set; }

            void IJsonArrayConvertible.FromArrayValues(ref Utf8JsonReader reader)
            {
                reader.Read();
                ColumnValue = AircloakColumnJsonParser.ParseBool(ref reader);
                reader.Read();
                Count = reader.GetInt64();
                reader.Read();
                if (reader.TokenType != JsonTokenType.Null)
                {
                    CountNoise = reader.GetDouble();
                }
            }
        }

        public class TextResult : IJsonArrayConvertible
        {
            public TextResult()
            {
                ColumnValue = new NullColumn<string>();
            }

            public AircloakColumn<string> ColumnValue { get; set; }

            public long? Count { get; set; }

            public double? CountNoise { get; set; }

            void IJsonArrayConvertible.FromArrayValues(ref Utf8JsonReader reader)
            {
                reader.Read();
                ColumnValue = AircloakColumnJsonParser.ParseString(ref reader);
                reader.Read();
                Count = reader.GetInt64();
                reader.Read();
                if (reader.TokenType != JsonTokenType.Null)
                {
                    CountNoise = reader.GetDouble();
                }
            }
        }
    }
}