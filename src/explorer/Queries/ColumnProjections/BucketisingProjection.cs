namespace Explorer.Queries
{
    using System;
    using System.Text.Json;
    using Diffix;
    using Explorer.Common;
    using Explorer.Components;

    public class BucketisingProjection : ColumnProjection
    {
        // TODO: this factor could be more dynamic, eg. based on a target value count per bucket.
        private const double BucketsWithinIRQ = 10;
        private readonly Random rng = new Random();

        public BucketisingProjection(
            string column,
            DValueType columnType,
            int index,
            NumericDistribution distribution)
        : base(column, index)
        {
            BucketSize = ComputeBucketSize(distribution.Quartiles.Item1, distribution.Quartiles.Item3);
            ColumnType = columnType;
        }

        public BucketSize BucketSize { get; }

        public DValueType ColumnType { get; }

        public override string Project() => $"bucket (\"{Column}\" by {BucketSize.SnappedSize})";

        /// <summary>
        /// The bucketising projection takes a column value and puts it in a bucket. In the reverse direction, we select
        /// a random value from the bucket assuming a uniform distribution of values within the bucket.
        /// </summary>
        /// <param name="value">The JsonElement containing the response, which is the lower bucket bound.</param>
        /// <returns>An object (or null) with a random value from the bucket.</returns>
        public override object? Invert(JsonElement value)
        {
            if (value.ValueKind == JsonValueKind.Null)
            {
                return null;
            }

            var lowerBound = value.GetDouble();
            var offset = rng.NextDouble() * Convert.ToDouble(BucketSize.SnappedSize);

            var result = lowerBound + offset;

            return ColumnType == DValueType.Integer
                ? Convert.ToInt64(result)
                : result;
        }

        /// <summary>
        /// Computes a bucket size based on inter-quartile range of the buckets.
        /// </summary>
        /// <param name="lowerQuartile">The lower quartile.</param>
        /// <param name="upperQuartile">The upper quartile.</param>
        /// <returns>An appropriate bucket size.</returns>
        private static BucketSize ComputeBucketSize(double lowerQuartile, double upperQuartile)
        {
            var interQuartileRange = upperQuartile - lowerQuartile;

            return new BucketSize(interQuartileRange / BucketsWithinIRQ);
        }
    }
}