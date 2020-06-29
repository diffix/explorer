namespace Explorer.Components
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json;
    using System.Text.Json.Serialization;

    using Accord.Statistics.Distributions.Univariate;
    using Explorer.Metrics;

    public class DescriptiveStatsPublisher : EmpiricalDistributionPublisher
    {
        public DescriptiveStatsPublisher(ResultProvider<EmpiricalDistribution> distributionProvider)
        : base(distributionProvider)
        {
        }

        protected override IEnumerable<ExploreMetric> EnumerateMetrics(EmpiricalDistribution distribution)
        {
            yield return new UntypedMetric(
                name: "descriptive_stats",
                metric: new Stats(distribution));
        }

        public class Stats
        {
            public Stats(EmpiricalDistribution distribution)
            {
                Entropy = distribution.Entropy;
                Mean = distribution.Mean;
                Mode = distribution.Mode;
                Quartiles = (
                    distribution.Quartiles.Min,
                    distribution.Median,
                    distribution.Quartiles.Max
                );
                StandardDeviation = distribution.StandardDeviation;
                Variance = distribution.Variance;
            }

            public double Entropy { get; }
            public double Mean { get; }
            public double Mode { get; }

            [JsonConverter(typeof(ThreeTupleConverter))]
            public (double, double, double) Quartiles { get; }
            public double StandardDeviation { get; }
            public double Variance { get; }
        }


        public class ThreeTupleConverter : JsonConverter<(double, double, double)>
        {
            public override (double, double, double) Read(
                ref Utf8JsonReader reader,
                Type typeToConvert,
                JsonSerializerOptions options)
            {
                (double, double, double) result;
                if (reader.TokenType == JsonTokenType.StartArray && reader.Read())
                {
                    result = (reader.GetDouble(), reader.GetDouble(), reader.GetDouble());
                    reader.Read();
                }
                else
                {
                    throw new JsonException("Couldn't read three-tuple from json.");
                }
                return result;
            }

            public override void Write(
                Utf8JsonWriter writer,
                (double, double, double) value,
                JsonSerializerOptions options)
            {
                writer.WriteStartArray();
                writer.WriteNumberValue(value.Item1);
                writer.WriteNumberValue(value.Item2);
                writer.WriteNumberValue(value.Item3);
                writer.WriteEndArray();
            }
        }
    }
}