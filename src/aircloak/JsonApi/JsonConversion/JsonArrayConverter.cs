namespace Aircloak.JsonApi
{
    using System.Text.Json;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Implements <see cref="JsonConverter"/> in terms of <see cref="IJsonArrayConvertible"/>.
    /// </summary>
    /// <typeparam name="T">A type that implements <see cref="IJsonArrayConvertible"/>.</typeparam>
    internal class JsonArrayConverter<T> : JsonConverter<T>
        where T : IJsonArrayConvertible, new()
    {
        public override T Read(
            ref Utf8JsonReader reader,
            System.Type typeToConvert,
            JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartArray)
            {
                throw new JsonException("Expected an array.");
            }

            var value = new T();
            value.FromArrayValues(ref reader);

            // Read ']' Token
            reader.Read();
            if (reader.TokenType != JsonTokenType.EndArray)
            {
                throw new JsonException($"Expected end of {typeof(T)} array.");
            }

            return value;
        }

        public override void Write(
            Utf8JsonWriter writer,
            T value,
            JsonSerializerOptions options)
        {
            writer.WriteStartArray();
            value.ToArrayValues(writer);
            writer.WriteEndArray();
        }
    }
}
