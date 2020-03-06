namespace Aircloak.JsonApi.JsonReaderExtensions
{
    using System.Text.Json;
    using System.Collections.Generic;

    using Aircloak.JsonApi.ResponseTypes;

    /// <summary>
    /// Extension methods for <see cref="Utf8JsonReader"/>.
    /// </summary>
    public static class JsonReaderExtensions
    {
        /// <summary>
        /// Delegate definition for a function that parses a value from a <see cref="Utf8JsonReader"/>.
        /// </summary>
        /// <param name="reader">The <see cref="Utf8JsonReader"/>.</param>
        /// <typeparam name="T">Type of the parsed value.</typeparam>
        /// <returns>A parsed value.</returns>
        public delegate T Utf8JsonValueParser<T>(ref Utf8JsonReader reader);

        /// <summary>
        /// Parses the result of a count() aggregate.
        /// </summary>
        /// <param name="reader">The <see cref="Utf8JsonReader"/>.</param>
        /// <returns>The COUNT value as a <c>long</c>.</returns>
        public static long ParseCount(this ref Utf8JsonReader reader)
        {
            return ParseNonNullableMetric<long>(ref reader);
        }

        /// <summary>
        /// Parses the result of the count_noise() aircloak function.
        /// </summary>
        /// <param name="reader">The <see cref="Utf8JsonReader"/>.</param>
        /// <returns>A value of type <c>double</c>, or null.</returns>
        public static double? ParseCountNoise(this ref Utf8JsonReader reader)
        {
            return ParseNullableMetric<double>(ref reader);
        }

        /// <summary>
        /// Parses a nullable aircloak metric.
        /// </summary>
        /// <param name="reader">The <see cref="Utf8JsonReader"/>.</param>
        /// <typeparam name="T">Type of the parsed value.</typeparam>
        /// <returns>A value of type T, or null.</returns>
        public static T? ParseNullableMetric<T>(this ref Utf8JsonReader reader)
            where T : unmanaged
        {
            return ParseNullableMetric(ref reader, DefaultParser<T>());
        }

        /// <summary>
        /// Parses a nullable aircloak metric using the provided parser if the value is non-null.
        /// </summary>
        /// <param name="reader">The <see cref="Utf8JsonReader"/>.</param>
        /// <param name="parseRawValue">A parser to use if the value is non-null.</param>
        /// <typeparam name="T">Type of the parsed value.</typeparam>
        /// <returns></returns>
        public static T? ParseNullableMetric<T>(this ref Utf8JsonReader reader, Utf8JsonValueParser<T> parseRawValue)
            where T : unmanaged
        {
            reader.Read();

            if (reader.TokenType == JsonTokenType.Null)
            {
                return null;
            }
            return parseRawValue(ref reader);
        }

        /// <summary>
        /// Check the token type of the next token in the sequence. Throw an exception if it doesn't match the provided
        /// <see cref="JsonTokenType"/>. \
        /// </summary>
        /// <param name="reader">The <see cref="Utf8JsonReader"/>.</param>
        /// <param name="token">The expected token type.</param>
        public static void Expect(this ref Utf8JsonReader reader, JsonTokenType token)
        {
            reader.Read();
            if (reader.TokenType != token)
            {
                throw new System.Exception($"Unxpected Json token: Expected {token}, got {reader.TokenType}.");
            }
        }

        /// <summary>
        /// Parses a non-nullable aircloak metric using the default parser for the provided type parameter 
        /// <c>T</c>.
        /// </summary>
        /// <param name="reader">The <see cref="Utf8JsonReader"/>.</param>
        /// <typeparam name="T">Type of the parsed value.</typeparam>
        /// <returns>A value of type T.</returns>
        public static T ParseNonNullableMetric<T>(this ref Utf8JsonReader reader)
        {
            return ParseNonNullableMetric(ref reader, DefaultParser<T>());
        }

        /// <summary>
        /// Parses a non-nullable aircloak metric using the provided parser.
        /// </summary>
        /// <param name="reader">The <see cref="Utf8JsonReader"/>.</param>
        /// <param name="parseRawValue">The parser to use.</param>
        /// <typeparam name="T">Type of the parsed value.</typeparam>
        /// <returns>A value of type T.</returns>
        public static T ParseNonNullableMetric<T>(this ref Utf8JsonReader reader, Utf8JsonValueParser<T> parseRawValue)
        {
            reader.Read();
            return parseRawValue(ref reader);
        }

        /// <summary>
        /// Parses a suppressible, nullable aircloak column value using the default parser for the provided type 
        /// parameter <c>T</c>.
        /// </summary>
        /// <param name="reader">The <see cref="Utf8JsonReader"/>.</param>
        /// <typeparam name="T">Type of the parsed value.</typeparam>
        /// <returns>The parsed value wrapped in an <see cref="AircloakValue{T}"/>.</returns>
        public static AircloakValue<T> ParseAircloakResultValue<T>(this ref Utf8JsonReader reader)
        {
            return ParseAircloakResultValue(ref reader, DefaultParser<T>());
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="reader">The <see cref="Utf8JsonReader"/>.</param>
        /// <param name="parseRawValue">A parser to use if the value is not suppressed and non-null.</param>
        /// <typeparam name="T">Type of the parsed value.</typeparam>
        /// <returns>The parsed value wrapped in an <see cref="AircloakValue{T}"/>.</returns>
        public static AircloakValue<T> ParseAircloakResultValue<T>(
            this ref Utf8JsonReader reader,
            Utf8JsonValueParser<T> parseRawValue)
        {
            reader.Read();
            return reader.TokenType switch
            {
                JsonTokenType.String when reader.ValueTextEquals("*") =>
                    SuppressedValue<T>.Instance,
                JsonTokenType.Null =>
                    NullValue<T>.Instance,
                _ => new DataValue<T>(parseRawValue(ref reader)),
            };
        }

        /// <summary>
        /// Get the default parser for a given type, if it has been defined.
        /// </summary>
        /// <typeparam name="T">Type of the parsed value.</typeparam>
        /// <returns>A <see cref="Utf8JsonValueParser{T}"/> for the given type.</returns>
        public static Utf8JsonValueParser<T> DefaultParser<T>()
        {
            if (defaultParsers.TryGetValue(typeof(T), out var parser))
            {
                return (Utf8JsonValueParser<T>)parser;
            }
            else
            {
                throw new System.Exception("No parser defined for {typeof(T)}");
            }
        }

        private static readonly Utf8JsonValueParser<string> RawStringParser =
            (ref Utf8JsonReader reader) => reader.GetString();

        private static readonly Utf8JsonValueParser<bool> RawBoolParser =
            (ref Utf8JsonReader reader) => reader.GetBoolean();

        private static readonly Utf8JsonValueParser<long> RawInt64Parser =
            (ref Utf8JsonReader reader) => reader.GetInt64();

        private static readonly Utf8JsonValueParser<int> RawInt32Parser =
            (ref Utf8JsonReader reader) => reader.GetInt32();

        private static readonly Utf8JsonValueParser<double> RawDoubleParser =
            (ref Utf8JsonReader reader) => reader.GetDouble();

        private static readonly Utf8JsonValueParser<decimal> RawDecimalParser =
            (ref Utf8JsonReader reader) => reader.GetDecimal();

        private static readonly Utf8JsonValueParser<char[]> RawCharArrayParser =
            (ref Utf8JsonReader reader) => reader.GetString().ToCharArray();

        private static readonly Utf8JsonValueParser<System.DateTime> RawDatetimeParser =
            (ref Utf8JsonReader reader) => reader.GetDateTime();

        private static readonly Dictionary<System.Type, object> defaultParsers = new Dictionary<System.Type, object> {
            { typeof(string), RawStringParser },
            { typeof(bool), RawBoolParser },
            { typeof(long), RawInt64Parser },
            { typeof(double), RawDoubleParser },
            { typeof(decimal), RawDecimalParser },
            { typeof(int), RawInt32Parser },
            { typeof(char[]), RawCharArrayParser },
            { typeof(System.DateTime), RawDatetimeParser },
        };
    }
}