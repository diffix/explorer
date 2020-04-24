namespace Explorer.Queries
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.Json;

    using Diffix;
    using Explorer.Common;

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
                from bucket in buckets select $"bucket({columnName} by {bucket})");

            QueryStatement = $@"
                select
                    grouping_id(
                        {bucketsFragment}
                    ),
                    {bucketsFragment},
                    count(*),
                    count_noise(*)
                from {tableName}
                group by grouping sets ({string.Join(",", Enumerable.Range(2, buckets.Count))})";

            Buckets = buckets.ToArray();
        }

        public string QueryStatement { get; }

        public decimal[] Buckets { get; }

        public Result ParseRow(ref Utf8JsonReader reader) =>
            new Result(ref reader, Buckets);

        public class Result : IndexedGroupingSetsResult<decimal, double>
        {
            public Result(ref Utf8JsonReader reader, decimal[] buckets)
            : base(ref reader, buckets)
            {
            }

            public decimal BucketSize => GroupingLabel;

            public DValue<double> LowerBound => GroupingValue;
        }
    }
}