#pragma warning disable CA1815 // Struct type should override Equals

namespace Aircloak.JsonApi.ResponseTypes
{
    /// <summary>
    /// Represents the JSON response from a request to /api/queries/{query_id}/cancel.
    /// </summary>
    public struct CancelResponse
    {
        /// <summary>
        /// Gets or sets a value indicating whether the query was succesfully canceled or not.
        /// </summary>
        public bool Success { get; set; }
    }
}

#pragma warning restore CA1815 // Struct type should override Equals
