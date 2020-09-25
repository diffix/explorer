namespace Explorer.Metrics
{
    using System.Text.Json.Serialization;

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum TextFormat
    {
        /// <summary>
        /// Text data has an unknown format.
        /// </summary>
        Unknonwn,

        /// <summary>
        /// Text data has email format.
        /// </summary>
        Email,
    }
}