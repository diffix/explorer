namespace Aircloak.JsonApi
{
    using System.Threading.Tasks;

    /// <summary>
    /// Provides an auth token for the Aircloak Json Api
    /// </summary>
    public interface IAircloakAuthenticationProvider
    {
        /// <summary>
        /// Returns a Task that resolves to api token string.
        /// </summary>
        public Task<string> GetAuthToken();
    }
}