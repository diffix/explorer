#pragma warning disable CA1812 // DValueTypeEnumConverter is an internal class that is apparently never instantiated.

namespace Diffix.JsonConversion
{
    using System;
    using System.Text.Json;
    using System.Text.Json.Serialization;

    using Diffix;

    /// <summary>
    /// <c>JsonConverter</c> for (de)serializing the <see cref="ValueType" /> enum as a string.
    /// </summary>
    public class DValueTypeEnumConverter : JsonConverter<DValueType>
    {
        private const string Integer = "integer";
        private const string Real = "real";
        private const string Text = "text";
        private const string Timestamp = "timestamp";
        private const string Date = "date";
        private const string Datetime = "datetime";
        private const string Bool = "boolean";

        public override DValueType Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return reader.GetString() switch
            {
                Integer => DValueType.Integer,
                Real => DValueType.Real,
                Text => DValueType.Text,
                Timestamp => DValueType.Timestamp,
                Date => DValueType.Date,
                Datetime => DValueType.Datetime,
                Bool => DValueType.Bool,
                _ => DValueType.Unknown,
            };
        }

        public override void Write(Utf8JsonWriter writer, DValueType aircloakType, JsonSerializerOptions options)
        {
            var s = aircloakType switch
            {
                DValueType.Integer => Integer,
                DValueType.Real => Real,
                DValueType.Text => Text,
                DValueType.Timestamp => Timestamp,
                DValueType.Date => Date,
                DValueType.Datetime => Datetime,
                DValueType.Bool => Bool,
                _ => "unknown",
            };
            writer.WriteStringValue(s);
        }
    }
}

#pragma warning restore CA1812 // AircloakTypeEnumConverter is an internal class that is apparently never instantiated.