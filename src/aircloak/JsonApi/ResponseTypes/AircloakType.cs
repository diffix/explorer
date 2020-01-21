#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
#pragma warning disable CA1720 // Identifier contains type name
#pragma warning disable SA1602 // Enumeration items should be documented

namespace Aircloak.JsonApi.ResponseTypes
{
    using System;
    using System.Text.Json;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Represents the different data types an Aircloak column can take.
    /// </summary>
    [JsonConverter(typeof(AircloakTypeEnumConverter))]
    public enum AircloakType
    {
        Integer,
        Real,
        Text,
        Timestamp,
        Date,
        Datetime,
        Bool,
        Unknown,
    }

    /// <summary>
    /// <c>JsonConverter</c> for (de)serializing the <c>AircloakType</c> enum as a string.
    /// </summary>
    public class AircloakTypeEnumConverter : JsonConverter<AircloakType>
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

#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
#pragma warning restore CS1720 // Identifier contains type name
#pragma warning restore SA1602 // Enumeration items should be documented