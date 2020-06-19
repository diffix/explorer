namespace Explorer.Common.JsonConversion
{
    using System;
    using System.Text.Json;
    using System.Text.Json.Serialization;

    public class ThreeDateTimesTupleConverter : JsonConverter<(DateTime, DateTime, DateTime)>
    {
        public override (DateTime, DateTime, DateTime) Read(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options)
        {
            (DateTime, DateTime, DateTime) result;
            if (reader.TokenType == JsonTokenType.StartArray && reader.Read())
            {
                result = (reader.GetDateTime(), reader.GetDateTime(), reader.GetDateTime());
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
            (DateTime, DateTime, DateTime) value,
            JsonSerializerOptions options)
        {
            writer.WriteStartArray();
            writer.WriteStringValue(value.Item1);
            writer.WriteStringValue(value.Item2);
            writer.WriteStringValue(value.Item3);
            writer.WriteEndArray();
        }
    }
}
