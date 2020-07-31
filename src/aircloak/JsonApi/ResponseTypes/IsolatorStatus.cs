namespace Aircloak.JsonApi.ResponseTypes
{
    using System.Text.Json.Serialization;
    using Aircloak.JsonApi.JsonConversion;

    /// <summary>
    /// The isolator status of a column. Can be known to be true or false, or can be
    /// "pending", "failed" or "unknown_column".
    /// </summary>
    [JsonConverter(typeof(IsolatorStatusConverter))]
    public class IsolatorStatus
    {
        internal IsolatorStatus(string status = "pending", bool isIsolator = true)
        {
            Status = status;
            IsIsolator = isIsolator;
        }

        /// <summary>
        /// Gets the status of the isolator column check.
        /// </summary>
        /// <value>Can be "ok", "pending", "failed" or "unknown_column".</value>
        public string Status { get; }

        /// <summary>
        /// Gets a value indicating whether or not this column should be classes as isolating.
        /// </summary>
        /// <value>Always <c>true</c> unless the column isolator check has successfully determined otherwise.</value>
        public bool IsIsolator { get; }
    }
}