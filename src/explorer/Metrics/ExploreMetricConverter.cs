namespace Explorer.Metrics
{
    using System;
    using System.Text.Json;
    using System.Text.Json.Serialization;

    public class ExploreMetricConverter : JsonConverter<ExploreMetric>
    {
        public override ExploreMetric Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            throw new InvalidOperationException("Don't try to read an ExploreMetric!");
        }

        public override void Write(Utf8JsonWriter writer, ExploreMetric value, JsonSerializerOptions options)
        {
            if (value.Invisible)
            {
                // Do nothing, it's invisible.
                return;
            }

            JsonSerializer.Serialize(
                writer,
                new
                {
                    name = value.Name,
                    value = value.Metric,
                },
                options);
        }
    }
}
