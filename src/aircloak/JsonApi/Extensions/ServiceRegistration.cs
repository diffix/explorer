namespace Aircloak.JsonApi
{
    using Microsoft.Extensions.DependencyInjection;

    /// <summary>
    /// Extension methods for service registration unsing Microsoft's dependecy injection framework.
    /// </summary>
    public static class ServiceRegistration
    {
        /// <summary>
        /// Injects and configures the <see cref="JsonApiClient"/>.
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
            return services
                .AddScoped<IAircloakAuthenticationProvider, TAuthHandler>()
                .AddHttpClient<JsonApiClient>(client => client.BaseAddress = apiBaseAddress);
        }
    }
}