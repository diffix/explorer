namespace Aircloak.JsonApi.ResponseTypes
{
    using System.Text.Json;

    /// <summary>
    /// Methods for Parsing <see cref="AircloakColumn{T}"/> values from Json.
    /// </summary>
    public static class AircloakColumnJsonParser
    {
        /// <summary>
        /// Encapsulates methods for reading and parsing Json elements.
        /// </summary>
        /// <param name="reader">An instance of <see cref="Utf8JsonReader"/>.</param>
        /// <typeparam name="T">The type of the value to be parsed.</typeparam>
        /// <returns>An instance of the parsed type.</returns>
        public delegate T Utf8JsonValueReader<T>(ref Utf8JsonReader reader);

        /// <summary>
        /// See <see cref="ParseColumn{T}"/>.
        /// </summary>
        /// <param name="reader">An instance of <see cref="Utf8JsonReader"/>.</param>
        /// <returns>An <see cref="AircloakColumn{T}"/> which may be suppressed or Null.</returns>
        /// <exception cref="System.Exception">
        /// Thrown by <see cref="ParseColumn{T}"/>.
        /// </exception>
        public static AircloakColumn<double> ParseDouble(ref Utf8JsonReader reader) =>
            ParseColumn(ref reader, JsonTokenType.Number, (ref Utf8JsonReader r) => r.GetDouble());

        /// <summary>
        /// See <see cref="ParseColumn{T}"/>.
        /// </summary>
        /// <param name="reader">An instance of <see cref="Utf8JsonReader"/>.</param>
        /// <returns>An <see cref="AircloakColumn{T}"/> which may be suppressed or Null.</returns>
        /// <exception cref="System.Exception">
        /// Thrown by <see cref="ParseColumn{T}"/>.
        /// </exception>
        public static AircloakColumn<decimal> ParseDecimal(ref Utf8JsonReader reader) =>
            ParseColumn(ref reader, JsonTokenType.Number, (ref Utf8JsonReader r) => r.GetDecimal());

        /// <summary>
        /// See <see cref="ParseColumn{T}"/>.
        /// </summary>
        /// <param name="reader">An instance of <see cref="Utf8JsonReader"/>.</param>
        /// <returns>An <see cref="AircloakColumn{T}"/> which may be suppressed or Null.</returns>
        /// <exception cref="System.Exception">
        /// Thrown by <see cref="ParseColumn{T}"/>.
        /// </exception>
        public static AircloakColumn<long> ParseLong(ref Utf8JsonReader reader) =>
            ParseColumn(ref reader, JsonTokenType.Number, (ref Utf8JsonReader r) => r.GetInt64());

        /// <summary>
        /// Parse Json to a <see cref="AircloakColumn{T}"/> instance, indicating whether the column value from a
        /// Diffix query has been anonmymized or returned a null value.
        /// </summary>
        /// <param name="reader">An instance of <see cref="Utf8JsonReader"/>.</param>
        /// <param name="expectedToken">The type of Json token expected if the value is not suppressed.</param>
        /// <param name="valueReader">The method to use to parse the value from Json.</param>
        /// <typeparam name="T">The type of the value to be parsed.</typeparam>
        /// <returns>An <see cref="AircloakColumn{T}"/> which may be suppressed or Null.</returns>
        public static AircloakColumn<T> ParseColumn<T>(
            ref Utf8JsonReader reader,
            JsonTokenType expectedToken,
            Utf8JsonValueReader<T> valueReader) =>
            reader.TokenType switch
            {
                JsonTokenType.String when reader.ValueTextEquals("*") =>
                    new SuppressedColumn<T>(),
                JsonTokenType.Null =>
                    new NullColumn<T>(),
                _ when reader.TokenType == expectedToken =>
                    new ValueColumn<T>(valueReader(ref reader)),
                _ => throw new System.Exception(
                    $"Unexpected Json token {reader.TokenType}. Expected {expectedToken}")
            };
    }
}