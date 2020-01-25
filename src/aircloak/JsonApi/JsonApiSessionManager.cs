namespace Aircloak.JsonApi
{
    using System;

    /// <summary>
    /// Contains the HttpClient instance and doles out ApiSession objects.
    /// </summary>
    public static class JsonApiSessionManager
    {
        private static readonly JsonApiClient ApiClient = new JsonApiClient();

        /// <summary>
        /// Creates a new <c>JsonApiSession</c> instance.
        /// </summary>
        /// <param name="apiRootUrl">The root Url for the Aircloak Api, eg. "https://attack.aircloak.com/api/".</param>
        /// <param name="apiKey">The Api key to use for this session.</param>
        /// <returns>The newly created instance.</returns>
        public static JsonApiSession NewJsonApiSession(Uri apiRootUrl, string apiKey)
        {
            return new JsonApiSession(ApiClient, apiRootUrl, apiKey);
        }
    }
}