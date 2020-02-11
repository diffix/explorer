using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("explorer.api.tests")]

namespace Explorer.Queries
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.Json;

    using Aircloak.JsonApi;
    using Aircloak.JsonApi.ResponseTypes;

    internal class SingleColumnHistogram :
        IQuerySpec<SingleColumnHistogram.Result>
    {
        public SingleColumnHistogram(
            string tableName,
            string columnName,
            IList<decimal> buckets)
        {
            TableName = tableName;
            ColumnName = columnName;
            Buckets = buckets;
        }

        public string QueryStatement
        {
            get
            {
                var bucketsFragment = string.Join(
                    ",",
                    from bucket in Buckets select $"bucket({ColumnName} by {bucket})");

                return $@"
                        select
                            grouping_id(
                                {bucketsFragment}
                            ),
                            {Buckets.Count} as num_buckets,
                            {bucketsFragment},
                            count(*),
                            count_noise(*)
                        from {TableName}
                        group by grouping sets ({Enumerable.Range(3, Buckets.Count)})";
            }
        }

        public string TableName { get; }

        public string ColumnName { get; }

        public IList<decimal> Buckets { get; }

        public class Result : IJsonArrayConvertible
        {
            public Result()
            {
                LowerBound = new NullColumn<decimal>();
            }

            public decimal? BucketSize { get; set; }

            public AircloakColumn<decimal> LowerBound { get; set; }

            public int? Count { get; set; }

            public double? CountNoise { get; set; }

            void IJsonArrayConvertible.FromArrayValues(ref Utf8JsonReader reader)
            {
                reader.Read();
                var groupingFlags = reader.GetInt32();
                reader.Read();
                var numBuckets = reader.GetInt32();

                for (var i = 0; i < numBuckets; i++)
                {
                    reader.Read();
                    if (((groupingFlags >> i) & 1) == 0)
                    {
                        LowerBound = AircloakColumnJsonParser.ParseDecimal(ref reader);
                    }
                }

                reader.Read();
                Count = reader.GetInt32();
                reader.Read();
                CountNoise = reader.GetInt32();
            }
        }
    }
}