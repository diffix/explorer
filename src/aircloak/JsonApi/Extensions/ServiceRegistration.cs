namespace Aircloak.JsonApi
{
    using Microsoft.Extensions.DependencyInjection;

    /// <summary>
    /// Extension methods for service registration unsing Microsoft's dependecy injection framework.
    /// </summary>
    public static class ServiceRegistration
    {
        /// <summary>
        /// Injects and configures the <see cref="JsonApiClient"/> with an authentication provider
        /// that can be resolved through DI.
        /// </summary>
        /// <param name="services">The IServiceCollection.</param>
        /// <param name="apiBaseAddress">The base Url for the aircloak api.</param>
        /// <typeparam name="TAuthHandler">
        /// A type that implements <see cref="IAircloakAuthenticationProvider"/>.
        /// </typeparam>
        /// <returns>The <see cref="IHttpClientBuilder"/> with the attached services.</returns>
        public static IHttpClientBuilder AddAircloakJsonApiServices<TAuthHandler>(
            this IServiceCollection services,
            System.Uri apiBaseAddress)
        where TAuthHandler : class, IAircloakAuthenticationProvider
        {
            var builder = services
                .AddHttpClient(JsonApiClient.HttpClientName)
                .ConfigureHttpClient(client => client.BaseAddress = apiBaseAddress);

            services
                .AddScoped<IAircloakAuthenticationProvider, TAuthHandler>()
                .AddScoped<JsonApiClient>();

            return builder;
        }

        /// <summary>
        /// Injects and configures the <see cref="JsonApiClient"/> with a given authentication
        /// provider instance.
        /// </summary>
        /// <param name="services">The IServiceCollection.</param>
        /// <param name="apiBaseAddress">The base Url for the aircloak api.</param>
        /// <param name="authProvider">The authentication provider instance.</param>
        /// <returns>The <see cref="IHttpClientBuilder"/> with the attached services.</returns>
        public static IHttpClientBuilder AddAircloakJsonApiServices(
            this IServiceCollection services,
            System.Uri apiBaseAddress,
            IAircloakAuthenticationProvider authProvider)
        {
            var builder = services
                .AddHttpClient(JsonApiClient.HttpClientName)
                .ConfigureHttpClient(client => client.BaseAddress = apiBaseAddress);

            services
                .AddScoped(_ => authProvider)
                .AddScoped<JsonApiClient>();

            return builder;
        }
    }
}