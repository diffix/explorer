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

    public sealed class ExplorerTestFixture : IDisposable
    {
        private const string ApiKeyEnvironmentVariable = "AIRCLOAK_API_KEY";

        public ExplorerTestFixture()
        {
            Container = new Container(registry =>
            {
                // VCR setup
                registry.For<IHttpClientFactory>().Use<VcrApiHttpClientFactory>().Scoped();
                registry.Injectable<VcrSharp.Cassette>();

                // Configure Authentication
                registry.For<IAircloakAuthenticationProvider>().Use(_ =>
                    StaticApiKeyAuthProvider.FromEnvironmentVariable(ApiKeyEnvironmentVariable));

                // Cancellation
                registry.Injectable<CancellationTokenSource>();

                // Singleton services
                registry.For<MetricsPublisher>().Use<SimpleMetricsPublisher>().Singleton();

                registry.IncludeRegistry<ComponentRegistry>();
            });
        }

        public Container Container { get; }

        public TestScope PrepareTestScope() => new TestScope(Container);

        public QueryableTestScope SimpleQueryTestScope(
            string dataSourceName,
            [CallerMemberName] string vcrFilename = "") =>
            PrepareTestScope()
                .LoadCassette(vcrFilename)
                .WithConnectionParams(dataSourceName);

#pragma warning disable CA2000 // Call System.IDisposable.Dispose on object (Allow calling context to dispose the scope.)
        public ComponentTestScope SimpleComponentTestScope(
            string dataSourceName,
            string tableName,
            string columnName,
            DValueType columnType = DValueType.Unknown,
            [CallerMemberName] string vcrFilename = "") =>
            PrepareTestScope()
                .LoadCassette(vcrFilename)
                .WithConnectionParams(dataSourceName)
                .WithContext(tableName, columnName, columnType);
#pragma warning restore CA2000 // Call System.IDisposable.Dispose on object

        public void Dispose()
        {
            Container.Dispose();
        }
    }
}