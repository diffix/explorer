namespace Aircloak.JsonApi
{
    using System;
    using System.Text.Json;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Implements <see cref="JsonConverter"/> in terms of <see cref="IJsonArrayConvertible"/>.
    /// </summary>
    /// <typeparam name="T">A type that implements <see cref="IJsonArrayConvertible"/>.</typeparam>
    /// <remarks>Note that this is meant for reading JSON only: the Write method is intentionally
    /// left unimplemented.</remarks>
    internal class JsonArrayConverter<TRowReader, T> : JsonConverter<T>
        where TRowReader : IRowReader<T>
    {
        private readonly IRowReader<T> querySpec;

        public JsonArrayConverter(IRowReader<T> querySpec)
        {
            this.querySpec = querySpec;
        }

        public override T Read(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartArray)
            {
                throw new JsonException("Expected an array.");
            }

            var value = querySpec.FromJsonArray(ref reader);

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
            throw new InvalidOperationException("This Type is for deserializing only!");
        }
    }
}
