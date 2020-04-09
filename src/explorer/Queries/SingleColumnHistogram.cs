namespace Explorer.Queries
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.Json;

    using Diffix;
    using Explorer.JsonExtensions;

    internal class SingleColumnHistogram :
        DQuery<SingleColumnHistogram.Result>
    {
        public SingleColumnHistogram(
            string tableName,
            string columnName,
            IList<decimal> buckets)
        {
            var bucketsFragment = string.Join(
                ",",
                from bucket in buckets select $"bucket({columnName} by {bucket}) as bucket_{(int)bucket}");

            var groupingIdArgs = string.Join(
                ",",
                from bucket in buckets select $"bucket({columnName} by {bucket})");

            QueryStatement = $@"
                select
                    grouping_id(
                        {groupingIdArgs}
                    ),
                    {buckets.Count} as num_buckets,
                    {bucketsFragment},
                    count(*),
                    count_noise(*)
                from {tableName}
                group by grouping sets ({string.Join(",", Enumerable.Range(3, buckets.Count))})";
        }

        public string QueryStatement { get; }

        public Result ParseRow(ref Utf8JsonReader reader)
        {
            var groupingFlags = reader.ParseNonNullableMetric<int>();
            var numBuckets = reader.ParseNonNullableMetric<int>();

            int? bucketIndex = null;
            DValue<decimal>? lowerBound = null;

            for (var i = numBuckets - 1; i >= 0; i--)
            {
                if (((groupingFlags >> i) & 1) == 0)
                {
                    bucketIndex = numBuckets - 1 - i;
                    lowerBound = reader.ParseDValue<decimal>();
                }
                else
                {
                    // discard value
                    reader.ParseDValue<decimal>();
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
                LowerBound = DValue<decimal>.Null;
            }

            public int BucketIndex { get; set; }

            public DValue<decimal> LowerBound { get; set; }

            public long Count { get; set; }

            public double? CountNoise { get; set; }
        }
    }
}