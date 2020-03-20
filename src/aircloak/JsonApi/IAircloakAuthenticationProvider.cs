namespace Aircloak.JsonApi
{
    using System.Threading.Tasks;

    /// <summary>
    /// Provides an auth token for the Aircloak Json Api.
    /// </summary>
    public interface IAircloakAuthenticationProvider
    {
        /// <summary>
        /// Returns a Task that resolves to api token string.
        /// </summary>
        /// <returns>A Task object that resolves to an api token string.</returns>
        public Task<string> GetAuthToken();
    }
}