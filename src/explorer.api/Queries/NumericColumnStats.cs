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

        string IQuerySpec<IntegerResult>.QueryStatement => throw new System.NotImplementedException();

        string IQuerySpec<RealResult>.QueryStatement => throw new System.NotImplementedException();

        IntegerResult IRowReader<IntegerResult>.FromJsonArray(ref Utf8JsonReader reader)
        {
            throw new System.NotImplementedException();
        }

        RealResult IRowReader<RealResult>.FromJsonArray(ref Utf8JsonReader reader)
        {
            throw new System.NotImplementedException();
        }

        public class IntegerResult
        {
            public long? Min { get; set; }

            public long? Max { get; set; }

            public long? Count { get; set; }

            public double? CountNoise { get; set; }

            void FromArrayValues(ref Utf8JsonReader reader)
            {
                reader.Read();
                Min = reader.GetInt64();
                reader.Read();
                Max = reader.GetInt64();
                reader.Read();
                Count = reader.GetInt64();
                reader.Read();
                CountNoise = reader.GetDouble();
            }
        }

        public class RealResult
        {
            public double? Min { get; set; }

            public double? Max { get; set; }

            public long? Count { get; set; }

            public double? CountNoise { get; set; }

            void FromArrayValues(ref Utf8JsonReader reader)
            {
                reader.Read();
                Min = reader.GetDouble();
                reader.Read();
                Max = reader.GetDouble();
                reader.Read();
                Count = reader.GetInt64();
                reader.Read();
                CountNoise = reader.GetDouble();
            }
        }
    }
}