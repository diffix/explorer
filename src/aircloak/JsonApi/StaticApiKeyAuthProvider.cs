namespace Aircloak.JsonApi
{
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Provides Aircloak authentication through a static api key.
    /// </summary>
    public class StaticApiKeyAuthProvider : IAircloakAuthenticationProvider
    {
        private string ApiKey { get; }

        /// <summary>
        /// Creates a new instance with the given <c>apiKey</c>.
        /// </summary>
        /// <param name="apiKey">The api key.</param>
        public StaticApiKeyAuthProvider(string apiKey)
        {
            ApiKey = apiKey;
        }

        /// <summary>
        /// Returns a <c>Task</c> that immediately resolves to the stored <c>apiKey</c>.
        /// </summary>
        /// <returns>A <c>Task</c> that immediately resolves to the stored <c>apiKey</c>.</returns>>
        public Task<string> GetAuthToken()
        {
            return Task.FromResult(ApiKey);
        }

        /// <summary>
        /// Creates a new <see cref="StaticApiKeyAuthProvider"/> with an api key retrieved from an environment
        /// variable.
        /// </summary>
        /// <param name="environmentVariable">The name of the environment variable.</param>
        /// <returns>A new <see cref="StaticApiKeyAuthProvider"/> containing the api key.</returns>
        public static StaticApiKeyAuthProvider FromEnvironmentVariable(string environmentVariable)
        {
            var apiKey = Environment.GetEnvironmentVariable(environmentVariable) ??
                throw new Exception($"Environment variable {environmentVariable} not set.");

            return new StaticApiKeyAuthProvider(apiKey);
        }
    }
}