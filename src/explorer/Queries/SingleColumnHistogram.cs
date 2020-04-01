namespace Explorer.Queries
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.Json;

    using Diffix;

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
            var groupingFlags = reader.ParseNonNullableMetric<int>();
            var numBuckets = reader.ParseNonNullableMetric<int>();

            int? bucketIndex = null;
            IDiffixValue<decimal>? lowerBound = null;

            for (var i = numBuckets - 1; i >= 0; i--)
            {
                if (((groupingFlags >> i) & 1) == 0)
                {
                    bucketIndex = numBuckets - 1 - i;
                    lowerBound = reader.ParseAircloakResultValue<decimal>();
                }
                else
                {
                    // discard value
                    reader.ParseAircloakResultValue<decimal>();
                }
            }

            var count = reader.ParseCount();
            var countNoise = reader.ParseCountNoise();

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

            public IDiffixValue<decimal> LowerBound { get; set; }

            public long Count { get; set; }

            public double? CountNoise { get; set; }
        }
    }
}