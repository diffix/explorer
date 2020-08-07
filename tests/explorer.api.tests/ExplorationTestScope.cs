namespace Explorer.Api.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    using Aircloak.JsonApi;
    using Explorer.Api.Authentication;
    using Explorer.Metrics;
    using Lamar;
    using VcrSharp;
    using Xunit;

    public class ExplorationTestScope
    {
        private static readonly Uri TestApiUri = new Uri("https://attack.aircloak.com/api/");
        private readonly Container rootContainer;

        public ExplorationTestScope(Container rootContainer)
        {
            this.rootContainer = rootContainer;
        }

        public Cassette? LoadedCassette { get; private set; }

        public ExplorationTestScope LoadCassette(string testFileName)
        {
            LoadedCassette?.Dispose();
            LoadedCassette = null;

            LoadedCassette = new Cassette($"../../../.vcr/{testFileName}.yaml");

            return this;
        }

        public async Task<Exploration> PrepareExploration(
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

            var testParams = new Models.ExploreParams
            {
                ApiUrl = apiUri.AbsoluteUri,
                DataSource = dataSource,
                Table = table,
                Columns = ImmutableArray.Create(columns.ToArray()),
            };

            // Create the Context and Connection objects for this exploration.
            var ctxList = await rootContainer.GetInstance<ContextBuilder>().Build(
                testParams,
                CancellationToken.None);

            var columnScopes = ctxList.Select(ctx =>
            {
                var scope = rootContainer.GetNestedContainer();
                if (LoadedCassette != null)
                {
                    scope.InjectDisposable(LoadedCassette);
                }

                return new TypeBasedScopeBuilder().Build(scope, ctx);
            });

            return new Exploration(dataSource, table, columnScopes);
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
            using var exploration = await PrepareExploration(dataSourceName, table, columns);

            await exploration.Completion;
            Assert.True(exploration.Completion.IsCompletedSuccessfully);
            Assert.True(exploration.ColumnExplorations.All(ce => ce.Completion.IsCompletedSuccessfully));

            CheckMetrics(exploration, check);
        }

        private static void CheckMetrics(
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
