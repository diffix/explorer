namespace Explorer.Api.Authentication
{
    using System.Threading.Tasks;

    using Aircloak.JsonApi;

    public class ExplorerApiAuthProvider : IAircloakAuthenticationProvider
    {
        private string apiKey;

        public ExplorerApiAuthProvider()
        {
            apiKey = "NO_API_KEY_REGISTERED";
        }

        public Task<string> GetAuthToken()
        {
            return Task.FromResult(apiKey);
        }

        public void RegisterApiKey(string apiKey)
        {
            this.apiKey = apiKey;
        }
    }
}