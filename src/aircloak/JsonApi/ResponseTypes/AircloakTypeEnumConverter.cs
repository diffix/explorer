#pragma warning disable CA1812 // AircloakTypeEnumConverter is an internal class that is apparently never instantiated.

namespace Aircloak.JsonApi.ResponseTypes
{
    using System;
    using System.Text.Json;
    using System.Text.Json.Serialization;

    /// <summary>
    /// <c>JsonConverter</c> for (de)serializing the <see cref="AircloakType" /> enum as a string.
    /// </summary>
    internal class AircloakTypeEnumConverter : JsonConverter<AircloakType>
    {
        private const string Integer = "integer";
        private const string Real = "real";
        private const string Text = "text";
        private const string Timestamp = "timestamp";
        private const string Date = "date";
        private const string Datetime = "datetime";
        private const string Bool = "boolean";

        public override AircloakType Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return reader.GetString() switch
            {
                Integer => AircloakType.Integer,
                Real => AircloakType.Real,
                Text => AircloakType.Text,
                Timestamp => AircloakType.Timestamp,
                Date => AircloakType.Date,
                Datetime => AircloakType.Datetime,
                Bool => AircloakType.Bool,
                _ => AircloakType.Unknown,
            };
        }

        public override void Write(Utf8JsonWriter writer, AircloakType aircloakType, JsonSerializerOptions options)
        {
            var s = aircloakType switch
            {
                AircloakType.Integer => Integer,
                AircloakType.Real => Real,
                AircloakType.Text => Text,
                AircloakType.Timestamp => Timestamp,
                AircloakType.Date => Date,
                AircloakType.Datetime => Datetime,
                AircloakType.Bool => Bool,
                _ => "unknown",
            };
            writer.WriteStringValue(s);
        }
    }
}

#pragma warning restore CA1812 // AircloakTypeEnumConverter is an internal class that is apparently never instantiated.