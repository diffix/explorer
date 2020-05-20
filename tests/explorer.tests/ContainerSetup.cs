namespace Explorer.Tests
{
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    using Aircloak.JsonApi;
    using Diffix;
    using Lamar;
    using Explorer.Common;
    using Explorer.Components;
    using Explorer.Metrics;

    using Microsoft.Extensions.DependencyInjection;

    public class ContainerSetup
    {
        const string ApiKeyEnvironmentVariable = "AIRCLOAK_API_KEY";
        const string UrlEnvironmentVariable = "AIRCLOAK_API_URL";
        const string DefaultTestUrl = "https://attack.aircloak.com/api/";

        public Container Container { get; }

        public ContainerSetup()
        {
            Container = new Container(registry =>
            {
                var urlString = System.Environment.GetEnvironmentVariable(UrlEnvironmentVariable)
                    ?? DefaultTestUrl;

                (registry as IServiceCollection)
                    .AddAircloakJsonApiServices(
                        new System.Uri(urlString),
                        StaticApiKeyAuthProvider.FromEnvironmentVariable(ApiKeyEnvironmentVariable));

                // Cancellation
                registry.Injectable<CancellationTokenSource>();

                // Singleton services
                registry.For<MetricsPublisher>().Use<SimpleMetricsPublisher>().Singleton();

                registry.IncludeRegistry<ComponentRegistry>();
            });
        }

        public TestScope PrepareTestScope() => new TestScope(Container);

        public QueryScope SimpleQueryScope(string dataSourceName) =>
            PrepareTestScope().WithConnectionParams(dataSourceName);
    }

    public class TestScope : QueryScope
    {
        private readonly CancellationTokenSource cts = new CancellationTokenSource();
        public INestedContainer Scope { get; }

        public TestScope(Container parentContainer)
        {
            Scope = parentContainer.GetNestedContainer();
            Scope.Inject(cts);
        }

        public TestScope WithConnectionParams(
            string dataSourceName,
            int pollFrequencySecs = 2,
            CancellationTokenSource? tokenSource = null)
        {
            Scope.Inject<DConnection>(
                new AircloakConnection(
                Scope.GetInstance<JsonApiClient>(),
                dataSourceName,
                System.TimeSpan.FromSeconds(pollFrequencySecs),
                tokenSource?.Token ?? CancellationToken.None));
            return this;
        }

        public TestScope WithContext(
            string tableName,
            string columnName,
            DValueType columnType = DValueType.Unknown)
        {
            Scope.Inject<ExplorerContext>(
                new RawExplorerContext
                {
                    Table = tableName,
                    Column = columnName,
                    ColumnType = columnType,
                });
            return this;
        }

        public async Task<IEnumerable<TRow>> QueryRows<TRow>(DQuery<TRow> query)
        {
            var queryResult = await Scope.GetInstance<DConnection>().Exec(query);

            return queryResult.Rows;
        }

        public void CancelQuery() => Scope.GetInstance<CancellationTokenSource>().Cancel();
    }

    public interface QueryScope
    {
        public Task<IEnumerable<TRow>> QueryRows<TRow>(DQuery<TRow> query);

        public void CancelQuery();

        public async Task CancelQuery(int millisecondDelay)
        {
            await Task.Delay(millisecondDelay);
            CancelQuery();
        }
    }
}