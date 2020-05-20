namespace Aircloak.JsonApi.JsonConversion
{
    using System;
    using System.Text.Json;
    using System.Text.Json.Serialization;

    using Diffix;

    /// <summary>
    /// Implements a <see cref="JsonConverter"/> for deserializing Aircloak rows from json array contents.
    /// </summary>
    /// <typeparam name="TQuery">A type that implements <see cref="DQuery{T}"/> for T.</typeparam>
    /// <typeparam name="TRow">The type that the json array will be converted to.</typeparam>
    /// <remarks>Note that this is meant for reading JSON only: the Write method is intentionally
    /// left unimplemented.</remarks>
    internal class JsonArrayConverter<TQuery, TRow> : JsonConverter<TRow>
        where TQuery : DQuery<TRow>
    {
        private readonly DQuery<TRow> query;

        public JsonArrayConverter(DQuery<TRow> query)
        {
            this.query = query;
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

            var value = query.ParseRow(ref reader);

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
