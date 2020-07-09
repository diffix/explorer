namespace Explorer.Api.Tests
{
    using System;
    using System.Net.Http;

    using Aircloak.JsonApi;
    using Explorer.Components;
    using Explorer.Metrics;
    using Lamar;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using VcrSharp;

    public sealed class ExplorationTestFixture : IDisposable
    {
        public static ExplorerConfig Config { get; } = new ConfigurationBuilder()
            .AddJsonFile("appsettings.Development.json")
            .AddEnvironmentVariables()
            .Build()
            .GetSection("Explorer")
            .Get<ExplorerConfig>();

        public ExplorationTestFixture()
        {
            RootContainer = new Container(registry =>
            {
                // VCR setup
                registry.For<IHttpClientFactory>().Use<VcrApiHttpClientFactory>().Scoped();
                registry.Injectable<Cassette>();

                // Configure Authentication
                registry.For<IAircloakAuthenticationProvider>().Use(Config);

                // Singleton services
                registry.AddLogging();

                // Scoped services
                registry
                    .AddScoped<MetricsPublisher, SimpleMetricsPublisher>()
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