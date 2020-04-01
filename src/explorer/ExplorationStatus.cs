namespace Explorer
{
    using System.Text.Json.Serialization;

#pragma warning disable SA1602 // Enumeration items should be documented
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum ExplorationStatus
    {
        New,
        Processing,
        Complete,
        Canceled,
        Error,
    }
#pragma warning restore
}