namespace Aircloak.JsonApi
{
    using System.Net.Http;
    using System.Threading.Tasks;

    /// <summary>
    /// Simplifies decalaration of a <see cref="DelegatingHandler"/> for Aircloak authentication.
    /// </summary>
    public abstract class AircloakAuthenticationHandler : DelegatingHandler
    {
        /// <summary>
        /// Initialise with an inner handler.
        /// </summary>
        /// <param name="innerHandler">The inner <see cref="DelegatingHandler"/>.</param>
        protected AircloakAuthenticationHandler(HttpMessageHandler innerHandler) : base(innerHandler)
        {
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        protected AircloakAuthenticationHandler() : base()
        {
        }

        /// <summary>
        /// Override this method with a task that returns the auth token.
        /// </summary>
        /// <returns>A Task that resolves to an api auth token string.</returns>
        protected abstract Task<string> GetAuthToken();

        /// <summary>
        /// Adds the auth token to the list of Http headers on the outgoing request.
        /// </summary>
        protected async override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            System.Threading.CancellationToken cancellationToken)
        {
            if (!request.Headers.TryAddWithoutValidation("auth-token", await GetAuthToken()))
            {
                throw new System.Exception($"Failed to add Http header 'auth-token'");
            }

            return await base.SendAsync(request, cancellationToken);
        }
    }
}