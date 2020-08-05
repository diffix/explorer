namespace Explorer.Queries
{
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;
    using System.Text.Json;

    using Diffix;
    using Explorer.Common;

    public class SingleColumnHistogram :
        DQuery<SingleColumnHistogram.Result>
    {
        private readonly ImmutableArray<decimal> buckets;

        public SingleColumnHistogram(IList<decimal> buckets)
        {
            this.buckets = buckets.ToImmutableArray();
        }

        public override Result ParseRow(ref Utf8JsonReader reader) =>
            new Result(ref reader, buckets);

        protected override string GetQueryStatement(string table, string column)
        {
            var bucketsFragment = string.Join(
                ",",
                from bucket in buckets select $"bucket({column} by {bucket})");

            return $@"
                select
                    grouping_id(
                        {bucketsFragment}
                    ),
                    {bucketsFragment},
                    count(*),
                    count_noise(*)
                from {table}
                group by grouping sets ({string.Join(",", Enumerable.Range(2, buckets.Length))})";
        }

        public class Result : IndexedGroupingSetsResult<decimal, double>
        {
            internal Result(ref Utf8JsonReader reader, ImmutableArray<decimal> buckets)
            : base(ref reader, buckets)
            {
            }

            public decimal BucketSize => GroupingLabel;

            public double LowerBound => Value;
        }
    }
}