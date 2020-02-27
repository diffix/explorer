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
                    from bucket in Buckets select $"bucket({ColumnName} by {bucket}) as bucket_{(int)bucket}");

                var groupingIdArgs = string.Join(
                    ",",
                    from bucket in Buckets select $"bucket({ColumnName} by {bucket})");

                return $@"
                        select
                            grouping_id(
                                {groupingIdArgs}
                            ),
                            {Buckets.Count} as num_buckets,
                            {bucketsFragment},
                            count(*),
                            count_noise(*)
                        from {TableName}
                        group by grouping sets ({string.Join(",", Enumerable.Range(3, Buckets.Count))})";
            }
        }

        private string TableName { get; }

        private string ColumnName { get; }

        private IList<decimal> Buckets { get; }

        public Result FromJsonArray(ref Utf8JsonReader reader)
        {
            reader.Read();
            var groupingFlags = reader.GetInt32();
            reader.Read();
            var numBuckets = reader.GetInt32();

            int? bucketIndex = null;
            AircloakValue<decimal>? lowerBound = null;

            for (var i = numBuckets - 1; i >= 0; i--)
            {
                reader.Read();
                if (((groupingFlags >> i) & 1) == 0)
                {
                    bucketIndex = numBuckets - 1 - i;
                    lowerBound = AircloakValueJsonParser.ParseDecimal(ref reader);
                }
            }

            reader.Read();
            var count = reader.GetInt32();
            reader.Read();
            var countNoise = reader.TokenType == JsonTokenType.Null ? 1.0
                                                                : reader.GetDouble();

            return new Result
            {
                BucketIndex = bucketIndex
                    ?? throw new System.Exception(
                        "Unable to retrieve bucket index from grouping flags."),
                LowerBound = lowerBound
                    ?? throw new System.Exception(
                        "Unable to parse columns return value, not even as Suppressed, Null."),
                Count = count,
                CountNoise = countNoise,
            };
        }

        public class Result
        {
            public Result()
            {
                LowerBound = NullValue<decimal>.Instance;
            }

            public int BucketIndex { get; set; }

            public AircloakValue<decimal> LowerBound { get; set; }

            public int Count { get; set; }

            public double? CountNoise { get; set; }
        }
    }
}