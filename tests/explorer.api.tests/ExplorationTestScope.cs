namespace Explorer.Api.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;
    using System.Threading.Tasks;

    using Aircloak.JsonApi;
    using Explorer.Api.Authentication;
    using Explorer.Metrics;
    using Lamar;
    using VcrSharp;
    using Xunit;

    public sealed class ExplorationTestScope : IDisposable
    {
        private static readonly Uri TestApiUri = new Uri("https://attack.aircloak.com/api/");
        private readonly INestedContainer scopedContainer;

        public ExplorationTestScope(Container rootContainer)
        {
            scopedContainer = rootContainer.GetNestedContainer();
        }

        private Cassette? LoadedCassette { get; set; }

        public ExplorationTestScope LoadCassette(string testFileName)
        {
            LoadedCassette = new Cassette($"../../../.vcr/{testFileName}.yaml");
            scopedContainer.InjectDisposable(LoadedCassette, replace: true);

            return this;
        }

        public Exploration RunExploration(
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
            if (scopedContainer.GetInstance<IAircloakAuthenticationProvider>() is ExplorerApiAuthProvider auth)
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

            var exploration = new Exploration((IContainer)scopedContainer, new TypeBasedScopeBuilder());
            exploration.Explore(scopedContainer.GetInstance<JsonApiContextBuilder>(), testParams);

            return exploration;
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
            using var exploration = RunExploration(dataSourceName, table, columns);

            await exploration.Completion;
            Assert.True(exploration.Completion.IsCompletedSuccessfully);
            Assert.True(exploration.ColumnExplorations.All(ce => ce.Completion.IsCompletedSuccessfully));

            CheckMetrics(exploration, check);
        }

        public void Dispose()
        {
            scopedContainer.Dispose();
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
