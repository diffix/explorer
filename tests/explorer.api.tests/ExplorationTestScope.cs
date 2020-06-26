namespace Explorer.Api.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    using Aircloak.JsonApi;
    using Explorer.Api.Authentication;
    using Explorer.Metrics;
    using Lamar;
    using VcrSharp;
    using Xunit;

    public class ExplorationTestScope : IDisposable
    {
        private const string TestApiUrl = "https://attack.aircloak.com/api/";
        private bool disposedValue;
        private readonly Container rootContainer;

        public ExplorationTestScope(Container rootContainer)
        {
            this.rootContainer = rootContainer;
        }

        public (string, Cassette)? LoadedCassette { get; private set; }

        public Dictionary<string, INestedContainer> ColumnScopes { get; }
            = new Dictionary<string, INestedContainer>();

        public ExplorationTestScope LoadCassette(string testFileName)
        {
            LoadedCassette?.Item2.Dispose();
            LoadedCassette = null;

            var cassette = new Cassette($"../../../.vcr/{testFileName}.yaml");
            LoadedCassette = (testFileName, cassette);

            foreach (var scope in ColumnScopes.Values)
            {
                scope.Inject(cassette);
            }

            return this;
        }

        public async Task RunExploration(
            string dataSource,
            string table,
            IEnumerable<string> columns,
            string apiUrl = TestApiUrl,
            string? apiKey = null)
        {

            // Register the authentication token for this scope.
            // Note that in most test cases the api key will not be needed as it will be provided from 
            // the environment (via a `StaticApiKeyAuthProvider`)
            if (rootContainer.GetInstance<IAircloakAuthenticationProvider>() is ExplorerApiAuthProvider auth)
            {
                auth.RegisterApiKey(apiKey ?? string.Empty);
            }

            // Create the Context and Connection objects for this exploration.
            var apiUri = new Uri(apiUrl);
            var ctxList = await rootContainer.GetInstance<ContextBuilder>().Build(apiUri, dataSource, table, columns);
            var conn = rootContainer.GetInstance<AircloakConnectionBuilder>().Build(apiUri, dataSource, CancellationToken.None);

            var columnExplorations = ctxList.Select(ctx =>
            {
                var scope = rootContainer.GetNestedContainer();
                if (LoadedCassette.HasValue)
                {
                    scope.Inject(LoadedCassette.Value.Item2);
                }

                ColumnScopes.Add(ctx.Column, scope);

                return ExplorationLauncher.ExploreColumn(
                    scope, conn, ctx, ComponentComposition.ColumnConfiguration(ctx.ColumnType));
            });

            var exploration = new Exploration(dataSource, table, columnExplorations.ToList());

            await exploration.Completion;
            Assert.True(exploration.Completion.IsCompletedSuccessfully);
            Assert.True(exploration.ColumnExplorations.All(ce => ce.Completion.IsCompletedSuccessfully));
        }

        public void CheckMetrics(Action<IEnumerable<ExploreMetric>> testMetrics)
        {
            INestedContainer scope;

            try
            {
                scope = ColumnScopes.Values.Single();
            }
            catch (InvalidOperationException)
            {
                throw new InvalidOperationException(
                    "`Expected a single-column test context");
            }

            var publisher = scope.GetInstance<MetricsPublisher>();

            testMetrics(publisher.PublishedMetrics);
        }

        public void CheckMetrics(Action<Dictionary<string, IEnumerable<ExploreMetric>>> testMetrics)
        {
            var metrics = new Dictionary<string, IEnumerable<ExploreMetric>>();
            foreach (var (column, scope) in ColumnScopes)
            {
                var publisher = scope.GetInstance<MetricsPublisher>();

                metrics.Add(column, publisher.PublishedMetrics);
            }

            testMetrics(metrics);
        }

        public async Task RunAndCheckMetrics(
            string dataSource,
            string table,
            string column,
            Action<IEnumerable<ExploreMetric>> testMetrics)
            => await RunAndCheckMetrics(
                dataSource,
                table,
                new[] { column },
                metricsDict => testMetrics(metricsDict[column]));

        public async Task RunAndCheckMetrics(
            string dataSourceName,
            string table,
            IEnumerable<string> columns,
            Action<Dictionary<string, IEnumerable<ExploreMetric>>> testMetrics)
        {
            await RunExploration(dataSourceName, table, columns);
            CheckMetrics(testMetrics);
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    DisposeDict(ColumnScopes);
                    LoadedCassette?.Item2.Dispose();
                    LoadedCassette = null;
                }

                disposedValue = true;
            }
        }

        private static void DisposeDict<T>(Dictionary<string, T> dict)
        where T : IDisposable
        {
            foreach (var scope in dict.Values)
            {
                scope.Dispose();
            }
            dict.Clear();
        }
    }
}
