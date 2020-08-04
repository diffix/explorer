#pragma warning disable CA1812 // IsolatorStatusConverter is an internal class that is apparently never instantiated. If so, remove the code from the assembly
namespace Aircloak.JsonApi.JsonConversion
{
    using System;
    using System.Text.Json;
    using System.Text.Json.Serialization;

    using Aircloak.JsonApi.ResponseTypes;

    internal class IsolatorStatusConverter : JsonConverter<IsolatorStatus>
    {
        public override IsolatorStatus Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            // All columns are to be treated as isolating until proven otherwise.
            var (isolator, status) = reader.TokenType switch
            {
                JsonTokenType.False => (false, "ok"),
                JsonTokenType.True => (true, "ok"),
                JsonTokenType.String => (true, reader.GetString()),
                _ => throw new JsonException("Expected either `true`, `false` or a string value.")
            };
            return new IsolatorStatus(status, isolator);
        }

        public override void Write(Utf8JsonWriter writer, IsolatorStatus value, JsonSerializerOptions options)
        {
            writer.WriteBooleanValue(value.IsIsolator);
        }
    }
}
#pragma warning restore CA1812 // IsolatorStatusConverter is an internal class that is apparently never instantiated. If so, remove the code from the assembly
