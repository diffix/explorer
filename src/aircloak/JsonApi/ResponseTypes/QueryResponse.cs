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
