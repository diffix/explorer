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
        public override AircloakType Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return reader.GetString() switch
            {
                "integer" => AircloakType.Integer,
                "real" => AircloakType.Real,
                "text" => AircloakType.Text,
                "timestamp" => AircloakType.Timestamp,
                "date" => AircloakType.Date,
                "datetime" => AircloakType.Datetime,
                "bool" => AircloakType.Bool,
                _ => AircloakType.Unknown,
            };
        }

        public override void Write(Utf8JsonWriter writer, AircloakType value, JsonSerializerOptions options)
        {
            var s = value switch
            {
                AircloakType.Integer => "integer",
                AircloakType.Real => "real",
                AircloakType.Text => "text",
                AircloakType.Timestamp => "timestamp",
                AircloakType.Date => "date",
                AircloakType.Datetime => "datetime",
                AircloakType.Bool => "bool",
                _ => "unknown",
            };
            writer.WriteStringValue(s);
        }
    }
}

#pragma warning restore CA1812 // AircloakTypeEnumConverter is an internal class that is apparently never instantiated.