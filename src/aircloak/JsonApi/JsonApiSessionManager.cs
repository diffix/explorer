namespace Aircloak.JsonApi
{
    using System;

    /// <summary>
    /// Contains the HttpClient instance and doles out ApiSession objects.
    /// </summary>
    public static class JsonApiSessionManager
    {
        private static readonly JsonApiClient ApiClient = new JsonApiClient();

        public static JsonApiSession NewJsonApiSession(Uri apiRootUrl, string apiKey)
        {
            return new JsonApiSession(ApiClient, apiRootUrl, apiKey);
        }
    }
}