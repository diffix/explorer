namespace Explorer.Api.Tests
{
    using System;
    using System.Net.Http;

    using Aircloak.JsonApi;
    using Explorer.Components;
    using Explorer.Metrics;
    using Lamar;
    using Microsoft.Extensions.DependencyInjection;
    using VcrSharp;

    public sealed class ExplorationTestFixture : IDisposable
    {
        private const string ApiKeyEnvironmentVariable = "AIRCLOAK_API_KEY";

        public ExplorationTestFixture()
        {
            RootContainer = new Container(registry =>
            {
                // VCR setup
                registry.For<IHttpClientFactory>().Use<VcrApiHttpClientFactory>().Scoped();
                registry.Injectable<Cassette>();

                // Configure Authentication
                registry.For<IAircloakAuthenticationProvider>().Use(_ =>
                    StaticApiKeyAuthProvider.FromEnvironmentVariable(ApiKeyEnvironmentVariable));

                // Singleton services
                registry.AddLogging();
                registry.For<MetricsPublisher>().Use<SimpleMetricsPublisher>().Scoped();

                // Scoped services
                registry
                    .AddScoped<ContextBuilder>()
                    .AddScoped<AircloakConnectionBuilder>();

                registry.IncludeRegistry<ComponentRegistry>();
            });
        }

        public Container RootContainer { get; }

        public ExplorationTestScope PrepareExplorationTestScope() => new ExplorationTestScope(RootContainer);

        public void Dispose()
        {
            RootContainer.Dispose();
        }
    }
}