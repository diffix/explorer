namespace Explorer.Common.JsonConversion
{
    using System;
    using System.Text.Json;
    using System.Text.Json.Serialization;

    public class ThreeDoublesTupleConverter : JsonConverter<(double, double, double)>
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
