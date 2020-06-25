using System.Linq;
namespace Explorer.Api.Tests
{
    using System;
    using System.Collections.Generic;
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

        public ExplorationTestScope(Container rootContainer)
        {
            Scope = rootContainer.GetNestedContainer();
        }

        public INestedContainer Scope { get; }

        public ExplorationTestScope LoadCassette(string testFileName)
        {
#pragma warning disable CA2000 // Call System.IDisposable.Dispose on object (Object lifetime is managed by container.)
            Scope.InjectDisposable(new Cassette($"../../../.vcr/{testFileName}.yaml"));
#pragma warning restore CA2000 // Call System.IDisposable.Dispose on object

            return this;
        }

        public async Task RunExploration(
            string dataSource,
            string table,
            string column,
            string apiUrl = TestApiUrl,
            string? apiKey = null)
        {
            // Register the authentication token for this scope.
            // Note that in most test cases the api key will not be needed as it will be provided from 
            // the environment (via a `StaticApiKeyAuthProvider`)
            if (Scope.GetInstance<IAircloakAuthenticationProvider>() is ExplorerApiAuthProvider auth)
            {
                auth.RegisterApiKey(apiKey ?? string.Empty);
            }

            // Create the Context and Connection objects for this exploration.
            var apiUri = new Uri(apiUrl);
            var ctxList = await Scope.GetInstance<ContextBuilder>().Build(apiUri, dataSource, table, new string[] { column });
            var conn = Scope.GetInstance<AircloakConnectionBuilder>().Build(apiUri, dataSource, CancellationToken.None);

            var explorations = ctxList.Select(ctx =>
            {
                return ExplorationLauncher.ExploreColumn(
                    Scope,
                    conn,
                    ctx,
                    ComponentComposition.ColumnConfiguration(ctx.ColumnType));
            }).ToList();

            await Task.WhenAll(explorations.Select(ce => ce.Completion));
            Assert.True(explorations.All(ce => ce.Completion.IsCompletedSuccessfully));
        }

        public void CheckMetrics(Action<IEnumerable<ExploreMetric>> testMetrics)
        {
            var publisher = Scope.GetInstance<MetricsPublisher>();

            testMetrics(publisher.PublishedMetrics);
        }

        public async Task RunAndCheckMetrics(
            string dataSourceName,
            string table,
            string column,
            Action<IEnumerable<ExploreMetric>> testMetrics)
        {
            await RunExploration(dataSourceName, table, column);
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
                    Scope.Dispose();
                }

                disposedValue = true;
            }
        }
    }
}
