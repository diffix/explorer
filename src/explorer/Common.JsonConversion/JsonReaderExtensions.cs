namespace Explorer.JsonExtensions
{
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;
    using System.Text.Json;

    using Diffix;
    using Explorer.Common;

    /// <summary>
    /// Extension methods for <see cref="Utf8JsonReader"/>.
    /// </summary>
    internal static class JsonReaderExtensions
    {
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

        private static readonly Utf8JsonValueParser<JsonElement> RawJsonElementParser =
            (ref Utf8JsonReader reader) =>
            {
                using var jdoc = JsonDocument.ParseValue(ref reader);
                return jdoc.RootElement.Clone();
            };

        private static readonly Dictionary<System.Type, object> DefaultParsers = new Dictionary<System.Type, object>
        {
            { typeof(string), RawStringParser },
            { typeof(bool), RawBoolParser },
            { typeof(long), RawInt64Parser },
            { typeof(double), RawDoubleParser },
            { typeof(decimal), RawDecimalParser },
            { typeof(int), RawInt32Parser },
            { typeof(char[]), RawCharArrayParser },
            { typeof(System.DateTime), RawDatetimeParser },
            { typeof(JsonElement), RawJsonElementParser },
        };

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
        /// Parses the result of the grouping_id() function.
        /// </summary>
        /// <param name="reader">The <see cref="Utf8JsonReader"/>.</param>
        /// <returns>The parsed grouping_id() result value.</returns>
        public static int ParseGroupingId(this ref Utf8JsonReader reader)
        {
            return ParseNonNullableMetric<int>(ref reader);
        }

        /// <summary>
        /// Parses a single value from a grouped set of values obtained via the `group by grouping sets` sql command.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Expects a single non-null value in the group. If multiple values are grouped, will return the last
        /// value from the group, *not* the whole group. The first value returned by the query must be the grouping_id.
        /// </para>
        /// <para>
        /// In other words, this is compatible with queries such as:
        /// <c>select grouping_id(a, b, c), a, b, c from myTable group by grouping sets (a, b, c)</c>.
        /// </para>
        /// <para>
        /// It incompatible with:
        /// <c>select grouping_id(a, b, c), a, b, c from myTable group by grouping sets ((a, b), (b, c), (a, c))</c>.
        /// </para>
        /// <para>Nor is it compatible with <c>group by cube</c> statements.</para>
        /// </remarks>
        /// <param name="reader">The <see cref="Utf8JsonReader"/>.</param>
        /// <param name="groupSize">The size of the grouped set.</param>
        /// <typeparam name="T">The type of the expected value from the group.</typeparam>
        /// <returns>The groupingId and the corresponding value.</returns>
        public static (int, DValue<T>) ParseGroupingSet<T>(this ref Utf8JsonReader reader, int groupSize)
        {
            var groupingId = reader.ParseGroupingId();
            var converter = GroupingIdConverter.GetConverter(groupSize);
            DValue<T>? groupValue = null;

            for (var i = 0; i < groupSize; i++)
            {
                if (converter.SingleIndexFromGroupingId(groupingId) == i)
                {
                    groupValue = reader.ParseDValue<T>();
                }
                else
                {
                    reader.Read();
                }
            }

            return (
                groupingId,
                groupValue ?? throw new System.Exception("Unable to Parse result from grouping set."));
        }

        /// <summary>
        /// Parses multiple values from a grouped set of values obtained via the `group by grouping sets` or
        /// `group by cube` sql command.
        /// </summary>
        /// <param name="reader">The <see cref="Utf8JsonReader"/>.</param>
        /// <param name="groupSize">The size of the grouped set.</param>
        /// <returns>The groupingId and the corresponding values as raw <see cref="JsonElement" />s.</returns>
        public static (int, ImmutableArray<DValue<JsonElement>>) ParseMultiGroupingSet(
            this ref Utf8JsonReader reader, int groupSize)
        {
            var groupingId = reader.ParseGroupingId();
            var converter = GroupingIdConverter.GetConverter(groupSize);

            var indices = converter.IndicesFromGroupingId(groupingId);
            var values = new List<DValue<JsonElement>>();

            for (var i = 0; i < groupSize; i++)
            {
                if (indices.Contains(i))
                {
                    values.Add(reader.ParseDValue<JsonElement>());
                }
                else
                {
                    reader.Read();
                }
            }

            return (groupingId, values.ToImmutableArray());
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
        /// Parses the result of a *_noise() aircloak function.
        /// </summary>
        /// <param name="reader">The <see cref="Utf8JsonReader"/>.</param>
        /// <returns>A value of type <c>double</c>, or null.</returns>
        public static double? ParseNoise(this ref Utf8JsonReader reader)
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
        /// Parses a nullable aircloak metric, returning a default value if the parsed return value is null.
        /// </summary>
        /// <param name="reader">The <see cref="Utf8JsonReader"/>.</param>
        /// <param name="defaultValue">The value to return if the parsed value is null.</param>
        /// <typeparam name="T">Type of the parsed value.</typeparam>
        /// <returns>A value of type T.</returns>
        public static T ParseNullableMetric<T>(this ref Utf8JsonReader reader, T defaultValue)
            where T : unmanaged
        {
            // We know that we are passinga non-null defaultValue so the return value will always be non-null.
            var metric = ParseNullableMetric(ref reader, DefaultParser<T>(), defaultValue);
            return metric!.Value;
        }

        /// <summary>
        /// Parses a nullable aircloak metric using the provided parser if the value is non-null. An optional
        /// default value can be passed and is returned if the parsed return value is null. If no default value
        /// is provided, returns <c>null</c>.
        /// </summary>
        /// <param name="reader">The <see cref="Utf8JsonReader"/>.</param>
        /// <param name="parseRawValue">A parser to use if the value is non-null.</param>
        /// <param name="defaultValue">The value to return if the parsed value is null.</param>
        /// <typeparam name="T">Type of the parsed value.</typeparam>
        /// <returns>A value of type T, or null.</returns>
        public static T? ParseNullableMetric<T>(
            this ref Utf8JsonReader reader,
            Utf8JsonValueParser<T> parseRawValue,
            T? defaultValue = null)
            where T : unmanaged
        {
            reader.Read();

            if (reader.TokenType == JsonTokenType.Null)
            {
                return defaultValue;
            }
            return parseRawValue(ref reader);
        }

        /// <summary>
        /// Check the token type of the next token in the sequence. Throw an exception if it doesn't match the provided
        /// <see cref="JsonTokenType"/>.
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
        /// <returns>The parsed value wrapped in an <see cref="DValue{T}"/>.</returns>
        public static DValue<T> ParseDValue<T>(this ref Utf8JsonReader reader)
        {
            return ParseDValue(ref reader, DefaultParser<T>());
        }

        /// <summary>
        /// Parses a suppressible, nullable aircloak column value using the provided parser for the wrapped value.
        /// </summary>
        /// <param name="reader">The <see cref="Utf8JsonReader"/>.</param>
        /// <param name="parseRawValue">A parser to use if the value is not suppressed and non-null.</param>
        /// <typeparam name="T">Type of the parsed value.</typeparam>
        /// <returns>The parsed value wrapped in an <see cref="DValue{T}"/>.</returns>
        public static DValue<T> ParseDValue<T>(
            this ref Utf8JsonReader reader,
            Utf8JsonValueParser<T> parseRawValue)
        {
            reader.Read();
            return reader.TokenType switch
            {
                JsonTokenType.String when reader.ValueTextEquals("*") => DValue<T>.Suppressed,
                JsonTokenType.Null => DValue<T>.Null,
                _ => DValue<T>.Create(parseRawValue(ref reader)),
            };
        }

        /// <summary>
        /// Get the default parser for a given type, if it has been defined.
        /// </summary>
        /// <typeparam name="T">Type of the parsed value.</typeparam>
        /// <returns>A <see cref="Utf8JsonValueParser{T}"/> for the given type.</returns>
        public static Utf8JsonValueParser<T> DefaultParser<T>()
        {
            if (DefaultParsers.TryGetValue(typeof(T), out var parser))
            {
                return (Utf8JsonValueParser<T>)parser;
            }
            else
            {
                throw new System.Exception("No parser defined for {typeof(T)}");
            }
        }
    }
}