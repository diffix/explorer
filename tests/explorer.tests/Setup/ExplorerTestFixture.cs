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
    using Explorer.Common;
    using Lamar;
    using Microsoft.Extensions.Configuration;
    using VcrSharp;

    public sealed class ExplorerTestFixture : IDisposable
    {
        private static readonly TestConfig Config = new ConfigurationBuilder()
            .AddJsonFile($"{Environment.CurrentDirectory}/../../../../appsettings.Test.json")
            .AddEnvironmentVariables()
            .Build()
            .GetSection("Explorer")
            .Get<TestConfig>();

        private static DataSourceCollection? dataSources;

        public ExplorerTestFixture()
        {
            Container = new Container(registry =>
            {
                // VCR setup
                registry.For<IHttpClientFactory>().Use<VcrApiHttpClientFactory>().Scoped();
                registry.Injectable<Cassette>();

                // Configure Authentication
                registry.For<IAircloakAuthenticationProvider>().Use(Config).Singleton();

                // Cancellation
                registry.Injectable<CancellationTokenSource>();

                // Publisher
                registry.For<MetricsPublisher>().Use<SimpleMetricsPublisher>().Scoped();

                registry.IncludeRegistry<ComponentRegistry>();
            });

            ApiUri = new Uri(Config.DefaultApiUrl);
        }

        public Uri ApiUri { get; }

        public Container Container { get; }

        public async Task<TestScope> CreateTestScope(
            string dataSource,
            string table,
            string column,
            object caller,
            RecordingOptions recordingOptions = RecordingOptions.SuccessOnly,
            VCRMode vcrMode = VCRMode.Cache,
            [CallerMemberName] string callerMemberName = "")
        {
            var vcrFileName = Cassette.GenerateVcrFilename(caller, callerMemberName);
            var columnInfo = await GetColumnInfo(dataSource, table, column);
            return new TestScope(Container, ApiUri, dataSource, table, column, columnInfo, vcrFileName, vcrMode, recordingOptions);
        }

        public async Task<TestScope> CreateTestScope(
            string dataSource,
            string table,
            IEnumerable<string> columns,
            object caller,
            RecordingOptions recordingOptions = RecordingOptions.SuccessOnly,
            VCRMode vcrMode = VCRMode.Cache,
            [CallerMemberName] string callerMemberName = "")
        {
            var vcrFileName = Cassette.GenerateVcrFilename(caller, callerMemberName);
            var columnInfo = await GetColumnInfo(dataSource, table, columns).Collect();
            return new TestScope(Container, ApiUri, dataSource, table, columns, columnInfo, vcrFileName, vcrMode, recordingOptions);
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
