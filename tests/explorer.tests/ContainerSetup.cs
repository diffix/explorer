namespace Explorer.Tests
{
    using System;
    using System.Net.Http;
    using System.Threading;
    using System.Runtime.CompilerServices;

    using Aircloak.JsonApi;
    using Diffix;
    using Lamar;
    using Explorer.Components;
    using Explorer.Metrics;

    public class ExplorerTestFixture : IDisposable
    {
        const string ApiKeyEnvironmentVariable = "AIRCLOAK_API_KEY";

        public Container Container { get; }

        public ExplorerTestFixture()
        {
            Container = new Container(registry =>
            {
                // VCR setup
                registry.For<IHttpClientFactory>().Use<VcrApiHttpClientFactory>().Scoped();
                registry.Injectable<VcrSharp.Cassette>();

                // Configure Authentication
                registry.For<IAircloakAuthenticationProvider>().Use(_ =>
                    StaticApiKeyAuthProvider.FromEnvironmentVariable(ApiKeyEnvironmentVariable)
                );

                // Cancellation
                registry.Injectable<CancellationTokenSource>();

                // Singleton services
                registry.For<MetricsPublisher>().Use<SimpleMetricsPublisher>().Singleton();

                registry.IncludeRegistry<ComponentRegistry>();
            });
        }

        public TestScope PrepareTestScope() => new TestScope(Container);

        public QueryableTestScope SimpleQueryTestScope(string dataSourceName, [CallerMemberName] string vcrFilename = "") =>
            PrepareTestScope()
                .LoadCassette(vcrFilename)
                .WithConnectionParams(dataSourceName);

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

        public void Dispose()
        {
            Container.Dispose();
        }
    }
}