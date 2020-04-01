#pragma warning disable CA1812 // DiffixTypeEnumConverter is an internal class that is apparently never instantiated.

namespace Aircloak.JsonApi.ResponseTypes
{
    using System;
    using System.Text.Json;
    using System.Text.Json.Serialization;

    using Diffix;

    /// <summary>
    /// <c>JsonConverter</c> for (de)serializing the <see cref="ValueType" /> enum as a string.
    /// </summary>
    internal class DiffixTypeEnumConverter : JsonConverter<ValueType>
    {
        private const string Integer = "integer";
        private const string Real = "real";
        private const string Text = "text";
        private const string Timestamp = "timestamp";
        private const string Date = "date";
        private const string Datetime = "datetime";
        private const string Bool = "boolean";

        public override ValueType Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return reader.GetString() switch
            {
                Integer => DiffixValueType.Integer,
                Real => DiffixValueType.Real,
                Text => DiffixValueType.Text,
                Timestamp => DiffixValueType.Timestamp,
                Date => DiffixValueType.Date,
                Datetime => DiffixValueType.Datetime,
                Bool => DiffixValueType.Bool,
                _ => DiffixValueType.Unknown,
            };
        }

        public override void Write(Utf8JsonWriter writer, ValueType aircloakType, JsonSerializerOptions options)
        {
            var s = aircloakType switch
            {
                DiffixValueType.Integer => Integer,
                DiffixValueType.Real => Real,
                DiffixValueType.Text => Text,
                DiffixValueType.Timestamp => Timestamp,
                DiffixValueType.Date => Date,
                DiffixValueType.Datetime => Datetime,
                DiffixValueType.Bool => Bool,
                _ => "unknown",
            };
            writer.WriteStringValue(s);
        }
    }
}

#pragma warning restore CA1812 // AircloakTypeEnumConverter is an internal class that is apparently never instantiated.