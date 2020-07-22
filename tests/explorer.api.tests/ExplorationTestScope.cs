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
        private static readonly Uri TestApiUri = new Uri("https://attack.aircloak.com/api/");
        private readonly Container rootContainer;
        private readonly List<INestedContainer> columnScopes = new List<INestedContainer>();
        private bool disposedValue;

        public ExplorationTestScope(Container rootContainer)
        {
            this.rootContainer = rootContainer;
        }

        public Cassette? LoadedCassette { get; private set; }

        public ExplorationTestScope LoadCassette(string testFileName)
        {
            LoadedCassette?.Dispose();
            LoadedCassette = null;

            var cassette = new Cassette($"../../../.vcr/{testFileName}.yaml");
            LoadedCassette = cassette;

            foreach (var scope in columnScopes)
            {
                scope.Inject(cassette);
            }

            return this;
        }

        public async Task<Exploration> RunExploration(
            string dataSource,
            string table,
            IEnumerable<string> columns,
            Uri? apiUri = null,
            string? apiKey = null)
        {
            apiUri ??= TestApiUri;
            apiKey ??= string.Empty;

            // Register the authentication token for this scope.
            // Note that in most test cases the api key will not be needed as it will be provided from
            // the environment (via a `StaticApiKeyAuthProvider`)
            if (rootContainer.GetInstance<IAircloakAuthenticationProvider>() is ExplorerApiAuthProvider auth)
            {
                auth.RegisterApiKey(apiKey);
            }

            // Create the Context and Connection objects for this exploration.
            var ctxList = await rootContainer.GetInstance<ContextBuilder>().Build(apiUri, dataSource, table, columns);
            var conn = rootContainer.GetInstance<AircloakConnectionBuilder>().Build(apiUri, dataSource, CancellationToken.None);

            var columnExplorations = ctxList.Select(ctx =>
            {
                var scope = rootContainer.GetNestedContainer();
                if (LoadedCassette != null)
                {
                    scope.Inject(LoadedCassette);
                }

                columnScopes.Add(scope);

                return ExplorationLauncher.ExploreColumn(
                    scope, conn, ctx, ComponentComposition.ColumnConfiguration(ctx.ColumnInfo.Type));
            });

            return new Exploration(dataSource, table, columnExplorations.ToList());
        }

        public async Task RunAndCheckMetrics(
            string dataSource,
            string table,
            string column,
            Action<IEnumerable<ExploreMetric>> check)
            => await RunAndCheckMetrics(
                dataSource,
                table,
                new[] { column },
                metricsDict => check(metricsDict[column]));

        public async Task RunAndCheckMetrics(
            string dataSourceName,
            string table,
            IEnumerable<string> columns,
            Action<Dictionary<string, IEnumerable<ExploreMetric>>> check)
        {
            var exploration = await RunExploration(dataSourceName, table, columns);

            await exploration.Completion;
            Assert.True(exploration.Completion.IsCompletedSuccessfully);
            Assert.True(exploration.ColumnExplorations.All(ce => ce.Completion.IsCompletedSuccessfully));

            CheckMetrics(exploration, check);
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
                    foreach (var scope in columnScopes)
                    {
                        scope.Dispose();
                    }
                    columnScopes.Clear();

                    LoadedCassette?.Dispose();
                    LoadedCassette = null;
                }

                disposedValue = true;
            }
        }

        private void CheckMetrics(
            Exploration exploration,
            Action<Dictionary<string, IEnumerable<ExploreMetric>>> check)
        {
            var metrics = exploration.ColumnExplorations.ToDictionary(
                ce => ce.Column,
                ce => ce.PublishedMetrics);

            check(metrics);
        }
    }
}
