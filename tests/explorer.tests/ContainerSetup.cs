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

        public QueryTestScope SimpleQueryTestScope(string dataSourceName) =>
            PrepareTestScope()
                .WithConnectionParams(dataSourceName);

        public ComponentTestScope SimpleComponentTestScope(
            string dataSourceName,
            string tableName,
            string columnName,
            DValueType columnType = DValueType.Unknown) =>
            PrepareTestScope()
                .WithConnectionParams(dataSourceName)
                .WithContext(tableName, columnName, columnType);
    }

    public class TestScope
    {
        protected readonly CancellationTokenSource cts = new CancellationTokenSource();
        public INestedContainer Container { get; }

        public TestScope(Container rootContainer)
        {
            Container = rootContainer.GetNestedContainer();
            Container.Inject(cts);
        }

        public QueryTestScope WithConnectionParams(
            string dataSourceName,
            int pollFrequencySecs = 2,
            CancellationTokenSource? tokenSource = null)
        {
            Container.Inject<DConnection>(
                new AircloakConnection(
                Container.GetInstance<JsonApiClient>(),
                dataSourceName,
                System.TimeSpan.FromSeconds(pollFrequencySecs),
                tokenSource?.Token ?? cts.Token));
            return new QueryTestScope(this);
        }
    }

    public class QueryTestScope
    {
        public QueryTestScope(TestScope scope)
        {
            Scope = scope;
        }

        public TestScope Scope { get; }

        public async Task<IEnumerable<TRow>> QueryRows<TRow>(DQuery<TRow> query)
        {
            var queryResult = await Scope.Container.GetInstance<DConnection>().Exec(query);

            return queryResult.Rows;
        }

        public void CancelQuery() => Scope.Container.GetInstance<CancellationTokenSource>().Cancel();

        public async Task CancelQuery(int millisecondDelay)
        {
            await Task.Delay(millisecondDelay);
            CancelQuery();
        }


        public ComponentTestScope WithContext(
            string tableName,
            string columnName,
            DValueType columnType = DValueType.Unknown)
        {
            Scope.Container.Inject<ExplorerContext>(
                new RawExplorerContext
                {
                    Table = tableName,
                    Column = columnName,
                    ColumnType = columnType,
                });
            return new ComponentTestScope(Scope);
        }
    }

    public class ComponentTestScope
    {
        public ComponentTestScope(TestScope scope)
        {
            Scope = scope;
        }

        public TestScope Scope { get; }

        public async Task Test<TComponent, TResult>(System.Action<TResult> test)
        where TComponent : ExplorerComponent<TResult>
        {
            var c = Scope.Container.GetInstance<TComponent>();
            var result = await c.ResultAsync;

            test(result);
        }
    }
}