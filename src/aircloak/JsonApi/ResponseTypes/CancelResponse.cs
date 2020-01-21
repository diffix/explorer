namespace Aircloak.JsonApi.ResponseTypes
{
    /// <summary>
    /// Represents the JSON response from a request to /api/queries/{query_id}/cancel.
    /// </summary>
    public struct CancelResponse
    {
        public bool Success { get; set; }
    }
}