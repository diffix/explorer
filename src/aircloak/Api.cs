namespace Aircloak
{
    using System;

    using Aircloak.JsonApi;

    /// <summary>
    /// Contains the HttpClient instance and doles out ApiSession objects.
    /// </summary>
    public static class Api
    {
        private static readonly JsonApiClient ApiClient = new JsonApiClient();

        public static JsonApiSession NewJsonApiSession(Uri apiRootUrl, string apiKey)
        {
            return new JsonApiSession(ApiClient, apiRootUrl, apiKey);
        }
    }
}