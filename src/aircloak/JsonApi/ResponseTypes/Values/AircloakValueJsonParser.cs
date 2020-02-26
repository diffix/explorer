namespace Aircloak.JsonApi.ResponseTypes
{
    using System.Text.Json;

    /// <summary>
    /// Methods for Parsing <see cref="AircloakValue{T}"/> values from Json.
    /// </summary>
    public static class AircloakValueJsonParser
    {
        /// <summary>
        /// Encapsulates methods for reading and parsing Json elements.
        /// </summary>
        /// <param name="reader">An instance of <see cref="Utf8JsonReader"/>.</param>
        /// <typeparam name="T">The type of the value to be parsed.</typeparam>
        /// <returns>An instance of the parsed type.</returns>
        private delegate T Utf8JsonValueReader<T>(ref Utf8JsonReader reader);

        /// <summary>
        /// See <see cref="ParseValue{T}"/>.
        /// </summary>
        /// <param name="reader">An instance of <see cref="Utf8JsonReader"/>.</param>
        /// <returns>An <see cref="AircloakValue{T}"/> which may be suppressed or Null.</returns>
        /// <exception cref="System.Exception">
        /// Thrown by <see cref="ParseValue{T}"/>.
        /// </exception>
        public static AircloakValue<double> ParseDouble(ref Utf8JsonReader reader) =>
            ParseValue(ref reader, JsonTokenType.Number, (ref Utf8JsonReader r) => r.GetDouble());

        /// <summary>
        /// See <see cref="ParseValue{T}"/>.
        /// </summary>
        /// <param name="reader">An instance of <see cref="Utf8JsonReader"/>.</param>
        /// <returns>An <see cref="AircloakValue{T}"/> which may be suppressed or Null.</returns>
        /// <exception cref="System.Exception">
        /// Thrown by <see cref="ParseValue{T}"/>.
        /// </exception>
        public static AircloakValue<decimal> ParseDecimal(ref Utf8JsonReader reader) =>
            ParseValue(ref reader, JsonTokenType.Number, (ref Utf8JsonReader r) => r.GetDecimal());

        /// <summary>
        /// See <see cref="ParseValue{T}"/>.
        /// </summary>
        /// <param name="reader">An instance of <see cref="Utf8JsonReader"/>.</param>
        /// <returns>An <see cref="AircloakValue{T}"/> which may be suppressed or Null.</returns>
        /// <exception cref="System.Exception">
        /// Thrown by <see cref="ParseValue{T}"/>.
        /// </exception>
        public static AircloakValue<long> ParseLong(ref Utf8JsonReader reader) =>
            ParseValue(ref reader, JsonTokenType.Number, (ref Utf8JsonReader r) => r.GetInt64());

        /// <summary>
        /// See <see cref="ParseValue{T}"/>.
        /// </summary>
        /// <param name="reader">An instance of <see cref="Utf8JsonReader"/>.</param>
        /// <returns>An <see cref="AircloakValue{T}"/> which may be suppressed or Null.</returns>
        /// <exception cref="System.Exception">
        /// Thrown by <see cref="ParseValue{T}"/>.
        /// </exception>
        public static AircloakValue<string> ParseString(ref Utf8JsonReader reader) =>
            ParseValue(ref reader, JsonTokenType.String, (ref Utf8JsonReader r) => r.GetString());

        /// <summary>
        /// Parse Json to a <see cref="AircloakValue{T}"/> instance, indicating whether the boolean value from a
        /// Diffix query has been anonmymized or returned a null value.
        /// </summary>
        /// <param name="reader">An instance of <see cref="Utf8JsonReader"/>.</param>
        /// <returns>An <c>AircloakColumn&lt;bool&gt;</c> which may be suppressed or Null.</returns>
        public static AircloakValue<bool> ParseBool(ref Utf8JsonReader reader) =>
            reader.TokenType switch
            {
                JsonTokenType.String when reader.ValueTextEquals("*") =>
                    SuppressedValue<bool>.Instance,
                JsonTokenType.Null =>
                    NullValue<bool>.Instance,
                JsonTokenType.True => new DataValue<bool>(true),
                JsonTokenType.False => new DataValue<bool>(false),
                _ => throw new System.Exception(
                    $"Unexpected Json token {reader.TokenType}. Expected boolean.")
            };

        /// <summary>
        /// Parse Json to a <see cref="AircloakValue{T}"/> instance, indicating whether the column value from a
        /// Diffix query has been anonmymized or returned a null value.
        /// </summary>
        /// <param name="reader">An instance of <see cref="Utf8JsonReader"/>.</param>
        /// <param name="expectedToken">The type of Json token expected if the value is not suppressed.</param>
        /// <param name="valueReader">The method to use to parse the value from Json.</param>
        /// <typeparam name="T">The type of the value to be parsed.</typeparam>
        /// <returns>An <see cref="AircloakValue{T}"/> which may be suppressed or Null.</returns>
        /// <exception>
        /// Throws a <see cref="System.Exception"/> if the Json value cannot be parsed as any of the following:
        /// <list type="bullet">
        /// <item>
        /// <description>A string indicating a suppressed value (Json String "*").</description>
        /// </item>
        /// <item>
        /// <description>A null value (Json null token).</description>
        /// </item>
        /// <item>
        /// <description>A value of the expected return type.</description>
        /// </item>
        /// </list>
        /// </exception>
        private static AircloakValue<T> ParseValue<T>(
            ref Utf8JsonReader reader,
            JsonTokenType expectedToken,
            Utf8JsonValueReader<T> valueReader) =>
            reader.TokenType switch
            {
                JsonTokenType.String when reader.ValueTextEquals("*") =>
                    SuppressedValue<T>.Instance,
                JsonTokenType.Null =>
                    NullValue<T>.Instance,
                _ when reader.TokenType == expectedToken =>
                    new DataValue<T>(valueReader(ref reader)),
                _ => throw new System.Exception(
                    $"Unexpected Json token {reader.TokenType}. Expected {expectedToken}")
            };
    }
}