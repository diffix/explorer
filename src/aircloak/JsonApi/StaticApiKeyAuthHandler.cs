namespace Aircloak.JsonApi
{
    using System;
    using System.Net.Http;
    using System.Threading.Tasks;

    /// <summary>
    /// Provides Aircloak authentication through a static api key.
    /// </summary>
    public class StaticApiKeyAuthHandler : AircloakAuthenticationHandler
    {
        private string ApiKey { get; }

        /// <summary>
        /// Creates a new instance with the given <c>apiKey</c>.
        /// </summary>
        /// <param name="apiKey">The api key.</param>
        public StaticApiKeyAuthHandler(string apiKey)
        {
            ApiKey = apiKey;
        }


        /// <summary>
        /// Creates a new instance with the given <c>apiKey</c>.
        /// </summary>
        /// <param name="innerHandler">The inner <see cref="HttpMessageHandler"/>.</param>
        /// <param name="apiKey">The api key.</param>
        public StaticApiKeyAuthHandler(HttpMessageHandler innerHandler, string apiKey)
        : base(innerHandler)
        {
            ApiKey = apiKey;
        }

        /// <summary>
        /// Returns a <c>Task</c> that immediately resolves to the stored <c>apiKey</c>.
        /// </summary>
        /// <returns>A <c>Task</c> that immediately resolves to the stored <c>apiKey</c>.</returns>>
        protected override Task<string> GetAuthToken()
        {
            return Task.FromResult(ApiKey);
        }

        /// <summary>
        /// Creates a new <see cref="StaticApiKeyAuthHandler"/> with an api key retrieved from an environment
        /// variable.
        /// </summary>
        /// <param name="environmentVariable">The name of the environment variable.</param>
        /// <param name="innerHandler">The inner HttpHandler, may be null.</param>
        /// <returns>A new <see cref="StaticApiKeyAuthHandler"/> containing the api key.</returns>
        public static StaticApiKeyAuthHandler FromEnvironmentVariable(
            string environmentVariable,
            HttpMessageHandler? innerHandler)
        {
            var apiKey = Environment.GetEnvironmentVariable(environmentVariable) ??
                throw new Exception($"Environment variable {environmentVariable} not set.");

            return innerHandler is null
                ? new StaticApiKeyAuthHandler(apiKey)
                : new StaticApiKeyAuthHandler(innerHandler, apiKey);
        }
    }
}