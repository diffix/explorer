namespace Explorer.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using System.Threading.Tasks;

    using Aircloak.JsonApi;
    using Aircloak.JsonApi.ResponseTypes;
    using Diffix;
    using Explorer.Components;
    using Explorer.Metrics;
    using Lamar;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using VcrSharp;

    public sealed class ExplorerTestFixture : IDisposable
    {
        private static readonly IConfiguration Config = new ConfigurationBuilder()
            .AddJsonFile("appsettings.Test.json")
            .AddEnvironmentVariables()
            .Build()
            .GetSection("Explorer");

        private static DataSourceCollection? dataSources;

        public ExplorerTestFixture()
        {
            ApiUri = new Uri(Config.Get<TestConfig>().DefaultApiUrl);

            Container = new Container(registry =>
            {
                // Configure options
                registry.Configure<ExplorerOptions>(Config);
                registry.Configure<ConnectionOptions>(Config);
                registry.Configure<VcrOptions>(Config);

                // Logging
                registry.AddLogging();

                // VCR setup
                registry.Injectable<Cassette>();
                registry.For<IHttpClientFactory>().Use<VcrApiHttpClientFactory>();

                // Configure Authentication
                registry.For<IAircloakAuthenticationProvider>().Use(Config.Get<TestConfig>()).Singleton();

                // Cancellation
                registry.Injectable<CancellationTokenSource>();

                // Publisher
                registry.For<MetricsPublisher>().Use<SimpleMetricsPublisher>().Scoped();

                registry.IncludeRegistry<ComponentRegistry>();
            });
        }

        public Uri ApiUri { get; }

        public Container Container { get; }

        public async Task<TestScope> CreateTestScope(
            string dataSource,
            string table,
            string column,
            object caller,
            [CallerMemberName] string callerMemberName = "")
        {
            var vcrFileName = Cassette.GenerateVcrFilename(caller, callerMemberName);
            var columnInfo = await GetColumnInfo(dataSource, table, column);
            var samplesToPublish = Config.Get<ExplorerOptions>().DefaultSamplesToPublish;
            return new TestScope(Container, ApiUri, dataSource, table, column, columnInfo, vcrFileName, samplesToPublish);
        }

        public async Task<TestScope> CreateTestScope(
            string dataSource,
            string table,
            IEnumerable<string> columns,
            object caller,
            [CallerMemberName] string callerMemberName = "")
        {
            var vcrFileName = Cassette.GenerateVcrFilename(caller, callerMemberName);
            var columnInfo = await GetColumnInfo(dataSource, table, columns).Collect();
            var samplesToPublish = Config.Get<ExplorerOptions>().DefaultSamplesToPublish;
            return new TestScope(Container, ApiUri, dataSource, table, columns, columnInfo, vcrFileName, samplesToPublish);
        }

        public void Dispose()
        {
            Container.Dispose();
        }

        private async Task<DColumnInfo> GetColumnInfo(string dataSource, string table, string column)
            => (await GetColumnInfo(dataSource, table, new[] { column }).Collect()).Single();

        private async IAsyncEnumerable<DColumnInfo> GetColumnInfo(string dataSource, string table, IEnumerable<string> columns)
        {
            var apiClient = Container.GetInstance<JsonApiClient>();
            if (dataSources == null)
            {
                dataSources = await apiClient.GetDataSources(ApiUri, CancellationToken.None);
            }

            if (!dataSources.AsDict.TryGetValue(dataSource, out var dataSourceInfo))
            {
                throw new ArgumentException($"Could not find datasource '{dataSource}'.");
            }

            if (!dataSourceInfo.TableDict.TryGetValue(table, out var tableInfo))
            {
                throw new ArgumentException($"Could not find table '{dataSource}.{table}'.");
            }

            foreach (var column in columns)
            {
                if (!tableInfo.ColumnDict.TryGetValue(column, out var columnInfo))
                {
                    throw new ArgumentException($"Could not find column '{dataSource}.{table}.{column}'.");
                }

                yield return new DColumnInfo(columnInfo.Type, columnInfo.UserId, columnInfo.Isolating.IsIsolator);
            }
        }

#pragma warning disable CA1812 // ExplorerTestFixture.TestConfig is an internal class that is apparently never instantiated.
        private class TestConfig : IAircloakAuthenticationProvider
        {
            public string AircloakApiKey { get; set; } = string.Empty;

            public string DefaultApiUrl { get; set; } = string.Empty;

            public Task<string> GetAuthToken() => Task.FromResult(AircloakApiKey);
        }
#pragma warning restore CA1812 // ExplorerTestFixture.TestConfig is an internal class that is apparently never instantiated.
    }
}
