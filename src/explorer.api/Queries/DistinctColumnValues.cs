namespace Explorer.Queries
{
    using System.Text.Json;

    using Aircloak.JsonApi;

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
            public long? ColumnValue { get; set; }

            public long? Count { get; set; }

            public double? CountNoise { get; set; }

            void IJsonArrayConvertible.FromArrayValues(ref Utf8JsonReader reader)
            {
                reader.Read();
                ColumnValue = reader.GetInt64();
                reader.Read();
                Count = reader.GetInt64();
                reader.Read();
                CountNoise = reader.GetDouble();
            }
        }

        public class RealResult : IJsonArrayConvertible
        {
            public double? ColumnValue { get; set; }

            public long? Count { get; set; }

            public double? CountNoise { get; set; }

            void IJsonArrayConvertible.FromArrayValues(ref Utf8JsonReader reader)
            {
                reader.Read();
                ColumnValue = reader.GetDouble();
                reader.Read();
                Count = reader.GetInt64();
                reader.Read();
                CountNoise = reader.GetDouble();
            }
        }

        public class BoolResult : IJsonArrayConvertible
        {
            public bool? ColumnValue { get; set; }

            public long? Count { get; set; }

            public double? CountNoise { get; set; }

            void IJsonArrayConvertible.FromArrayValues(ref Utf8JsonReader reader)
            {
                reader.Read();
                ColumnValue = reader.GetBoolean();
                reader.Read();
                Count = reader.GetInt64();
                reader.Read();
                CountNoise = reader.GetDouble();
            }
        }

        public class TextResult : IJsonArrayConvertible
        {
            public string? ColumnValue { get; set; }

            public long? Count { get; set; }

            public double? CountNoise { get; set; }

            void IJsonArrayConvertible.FromArrayValues(ref Utf8JsonReader reader)
            {
                reader.Read();
                ColumnValue = reader.GetString();
                reader.Read();
                Count = reader.GetInt64();
                reader.Read();
                CountNoise = reader.GetDouble();
            }
        }
    }
}