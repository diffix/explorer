namespace Explorer.Tests
{
    using System;
    using System.Net.Http;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using System.Threading.Tasks;
    using Aircloak.JsonApi;
    using Diffix;
    using Explorer.Components;
    using Explorer.Metrics;
    using Lamar;
    using Microsoft.Extensions.Configuration;
    using VcrSharp;

    public sealed class ExplorerTestFixture : IDisposable
    {
        private static readonly TestConfig Config = new ConfigurationBuilder()
            .AddJsonFile($"{Environment.CurrentDirectory}/../../../../appsettings.Test.json")
            .AddEnvironmentVariables()
            .Build()
            .GetSection("Explorer")
            .Get<TestConfig>();

        public ExplorerTestFixture()
        {
            Container = new Container(registry =>
            {
                // VCR setup
                registry.For<IHttpClientFactory>().Use<VcrApiHttpClientFactory>().Scoped();
                registry.Injectable<Cassette>();

                // Configure Authentication
                registry.For<IAircloakAuthenticationProvider>().Use(Config).Singleton();

                // Cancellation
                registry.Injectable<CancellationTokenSource>();

                // Publisher
                registry.For<MetricsPublisher>().Use<SimpleMetricsPublisher>().Scoped();

                registry.IncludeRegistry<ComponentRegistry>();
            });

            ApiUri = new Uri(Config.DefaultApiUrl);
        }

        public Uri ApiUri { get; }

        public Container Container { get; }

        public static string GenerateVcrFilename(object caller, [CallerMemberName] string name = "") =>
            Cassette.GenerateVcrFilename(caller, name);

        public TestScope PrepareTestScope() => new TestScope(Container);

        public QueryableTestScope SimpleQueryTestScope(
            string dataSource,
            string vcrFilename) =>
            PrepareTestScope()
                .LoadCassette(vcrFilename)
                .WithConnectionParams(ApiUri, dataSource);

#pragma warning disable CA2000 // Call System.IDisposable.Dispose on object (Allow calling context to dispose the scope.)
        public ComponentTestScope SimpleComponentTestScope(
            string dataSource,
            string table,
            string column,
            string vcrFilename,
            DValueType columnType = DValueType.Unknown) =>
            PrepareTestScope()
                .LoadCassette(vcrFilename)
                .WithConnectionParams(ApiUri, dataSource)
                .WithContext(dataSource, table, column, columnType);
#pragma warning restore CA2000 // Call System.IDisposable.Dispose on object

        public void Dispose()
        {
            Container.Dispose();
        }

        private class TestConfig : IAircloakAuthenticationProvider
        {
            public string AircloakApiKey { get; set; } = string.Empty;

            public string DefaultApiUrl { get; set; } = string.Empty;

            public Task<string> GetAuthToken() => Task.FromResult(AircloakApiKey);
        }
    }
}