namespace Aircloak.JsonApi.JsonConversion
{
    using System;
    using System.Text.Json;
    using System.Text.Json.Serialization;

    using Diffix;

    /// <summary>
    /// Implements a <see cref="JsonConverter"/> for deserializing Aircloak rows from json array contents.
    /// </summary>
    /// <typeparam name="TRow">The type that the json array will be converted to.</typeparam>
    /// <remarks>Note that this is meant for reading JSON only: the Write method is intentionally
    /// left unimplemented.</remarks>
    internal class JsonArrayConverter<TRow> : JsonConverter<TRow>
    {
        private readonly DRowParser<TRow> rowParser;

        public JsonArrayConverter(DRowParser<TRow> rowParser)
        {
            this.rowParser = rowParser;
        }

        public override TRow Read(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartArray)
            {
                throw new JsonException("Expected an array.");
            }

            var value = rowParser(ref reader);

            // Read ']' Token
            reader.Read();
            if (reader.TokenType != JsonTokenType.EndArray)
            {
                throw new JsonException($"Expected end of {typeof(TRow)} array, got {reader.TokenType} token: `{reader.GetString()}`.");
            }

            return value;
        }

        public override void Write(
            Utf8JsonWriter writer,
            TRow value,
            JsonSerializerOptions options)
        {
            throw new InvalidOperationException("This Type is for deserializing only!");
        }
    }
}
