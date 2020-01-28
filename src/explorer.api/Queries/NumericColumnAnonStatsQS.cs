namespace Explorer
{
    using System.Text.Json;
    using Explorer.Api.Models;

    using Aircloak.JsonApi;

    internal class NumericColumnAnonStatsQS :
        IQuerySpec<NumericColumnAnonStatsQS.IntegerResult>,
        IQuerySpec<NumericColumnAnonStatsQS.RealResult>
    {
        public NumericColumnAnonStatsQS(ExploreParams exploreParams)
        {
            TableName = exploreParams.TableName;
            ColumnName = exploreParams.ColumnName;
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

        public class IntegerResult : IJsonArrayConvertible
        {
            public long? Min { get; set; }

            public long? Max { get; set; }

            public long? Count { get; set; }

            public double? CountNoise { get; set; }

            void IJsonArrayConvertible.FromArrayValues(ref Utf8JsonReader reader)
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

        public class RealResult : IJsonArrayConvertible
        {
            public double? Min { get; set; }

            public double? Max { get; set; }

            public long? Count { get; set; }

            public double? CountNoise { get; set; }

            void IJsonArrayConvertible.FromArrayValues(ref Utf8JsonReader reader)
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
