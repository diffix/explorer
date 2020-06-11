namespace Explorer.Api.Authentication
{
    using System.Threading.Tasks;

    using Aircloak.JsonApi;

    public class ExplorerApiAuthProvider : IAircloakAuthenticationProvider
    {
        private string? apiKey;

        public Task<string> GetAuthToken()
        {
            return Task.FromResult(apiKey ??
                throw new System.Exception($"No auth token registered in {nameof(ExplorerApiAuthProvider)}"));
        }

        public void RegisterApiKey(string apiKey)
        {
            this.apiKey = apiKey;
        }
    }
}