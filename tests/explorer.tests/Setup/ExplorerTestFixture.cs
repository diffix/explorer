namespace Explorer.Tests
{
    using System;
    using System.Net.Http;
    using System.Runtime.CompilerServices;
    using System.Threading;

    using Aircloak.JsonApi;
    using Diffix;
    using Explorer.Components;
    using Explorer.Metrics;
    using Lamar;
    using VcrSharp;

    public sealed class ExplorerTestFixture : IDisposable
    {
        public static readonly Uri ApiUrl = new Uri("https://attack.aircloak.com/api/");
        private const string ApiKeyEnvironmentVariable = "AIRCLOAK_API_KEY";

        public ExplorerTestFixture()
        {
            Container = new Container(registry =>
            {
                // VCR setup
                registry.For<IHttpClientFactory>().Use<VcrApiHttpClientFactory>().Scoped();
                registry.Injectable<Cassette>();

                // Configure Authentication
                registry.For<IAircloakAuthenticationProvider>().Use(_ =>
                    StaticApiKeyAuthProvider.FromEnvironmentVariable(ApiKeyEnvironmentVariable));

                // Cancellation
                registry.Injectable<CancellationTokenSource>();

                // Publisher
                registry.For<MetricsPublisher>().Use<SimpleMetricsPublisher>().Scoped();

                registry.IncludeRegistry<ComponentRegistry>();
            });
        }

        public Container Container { get; }

        public static string GenerateVcrFilename(object caller, [CallerMemberName] string name = "") =>
            Cassette.GenerateVcrFilename(caller, name);

        public TestScope PrepareTestScope() => new TestScope(Container);

        public QueryableTestScope SimpleQueryTestScope(
            string dataSource,
            string vcrFilename) =>
            PrepareTestScope()
                .LoadCassette(vcrFilename)
                .WithConnectionParams(ApiUrl, dataSource);

#pragma warning disable CA2000 // Call System.IDisposable.Dispose on object (Allow calling context to dispose the scope.)
        public ComponentTestScope SimpleComponentTestScope(
            string dataSource,
            string table,
            string column,
            string vcrFilename,
            DValueType columnType = DValueType.Unknown) =>
            PrepareTestScope()
                .LoadCassette(vcrFilename)
                .WithConnectionParams(ApiUrl, dataSource)
                .WithContext(dataSource, table, column, columnType);
#pragma warning restore CA2000 // Call System.IDisposable.Dispose on object

        public void Dispose()
        {
            Container.Dispose();
        }
    }
}