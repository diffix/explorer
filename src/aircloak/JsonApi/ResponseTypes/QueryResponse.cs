#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
#pragma warning disable CA1815 // Struct type should override Equals

namespace Aircloak.JsonApi.ResponseTypes
{
    /// <summary>
    /// Represents the JSON response from a POST request to /api/query.
    /// </summary>
    public struct QueryResponse
    {
        public bool Success { get; set; }

        public string QueryId { get; set; }

        public override string ToString()
        {
            return Success
                ? $"Query Accepted, ID: {QueryId}"
                : $"Query Failed";
        }
    }
}

#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
#pragma warning restore CA1815 // Struct type should override Equals
