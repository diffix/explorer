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
        public ExplorationTestFixture()
        {
            RootContainer = new Container(registry =>
            {
                // Configure options
                registry.Configure<ExplorerOptions>(Config);
                registry.Configure<ConnectionOptions>(Config);
                registry.Configure<VcrOptions>(Config);

                // VCR setup
                registry.Injectable<Cassette>();
                registry.For<IHttpClientFactory>().Use<VcrApiHttpClientFactory>();

                // Configure Authentication
                registry.For<IAircloakAuthenticationProvider>().Use(Config.Get<TestConfig>());

                // Singleton services
                registry.AddLogging();

                // Scoped services
                registry
                    .AddScoped<MetricsPublisher, SimpleMetricsPublisher>()
                    .AddScoped<JsonApiContextBuilder>()
                    .AddScoped<AircloakConnectionBuilder>();

                registry.IncludeRegistry<ComponentRegistry>();
            });
        }

        public Container RootContainer { get; }

        internal static IConfiguration Config { get; } = new ConfigurationBuilder()
            .AddJsonFile("appsettings.Development.json")
            .AddEnvironmentVariables()
            .Build()
            .GetSection("Explorer");

        public ExplorationTestScope PrepareExplorationTestScope() => new ExplorationTestScope(RootContainer);

        public ExplorationTestScope PrepareExplorationTestScope(string cassetteFilename) =>
            PrepareExplorationTestScope().LoadCassette(cassetteFilename);

        public void Dispose()
        {
            RootContainer.Dispose();
        }
    }
}