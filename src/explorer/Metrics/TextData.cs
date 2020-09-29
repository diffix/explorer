namespace Explorer.Metrics
{
    using System.Collections.Generic;

    using System.Text.Json.Serialization;

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum TextFormat
    {
        /// <summary>
        /// Text data has an unknown format.
        /// </summary>
        Unknown,

        /// <summary>
        /// Text data has email format.
        /// </summary>
        Email,
    }

    public class TextData
    {
        public TextData(TextFormat format, LengthsDistributionType distribution, ValueCounts? valueCounts)
        {
            Format = format;
            LengthsDistribution = distribution;
            LengthValueCounts = valueCounts;
        }

        public LengthsDistributionType LengthsDistribution { get; }

        public ValueCounts? LengthValueCounts { get; }

        public TextFormat Format { get; }

        public class LengthsDistributionType : List<ValueWithCount<long>>
        {
            public LengthsDistributionType(IEnumerable<ValueWithCount<long>> items)
                : base(items)
            {
            }
        }
    }
}
