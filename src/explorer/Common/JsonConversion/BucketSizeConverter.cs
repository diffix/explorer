namespace Explorer.Common.JsonConversion
{
    using System;
    using System.Text.Json;
    using System.Text.Json.Serialization;

    public class BucketSizeConverter : JsonConverter<BucketSize>
    {
        public override BucketSize Read(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options) =>
                new BucketSize(reader.GetDecimal());

        public override void Write(
            Utf8JsonWriter writer,
            BucketSize value,
            JsonSerializerOptions options) =>
                writer.WriteNumberValue(value.SnappedSize);
    }
}