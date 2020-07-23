namespace Explorer
{
    using System.Text.Json.Serialization;

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum ExplorationStatus
    {
        /// <summary>
        /// Waiting to be run.
        /// </summary>
        New,

        /// <summary>
        /// Running.
        /// </summary>
        Processing,

        /// <summary>
        /// Completed Successfully.
        /// </summary>
        Complete,

        /// <summary>
        /// Completed due due cancellation.
        /// </summary>
        Canceled,

        /// <summary>
        /// Completed with errors.
        /// </summary>
        Error,
    }
}