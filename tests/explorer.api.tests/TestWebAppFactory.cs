namespace Explorer.Api.Tests
{
    using System;
    using System.IO;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Text.Json;
    using System.Threading.Tasks;

    using Aircloak.JsonApi;
    using Explorer.Api;
    using Explorer.Api.Authentication;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Mvc.Testing;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Options;
    using VcrSharp;

    public sealed class TestWebAppFactory : WebApplicationFactory<Startup>
    {
        private static readonly IConfiguration Config = new ConfigurationBuilder()
            .AddJsonFile($"{Environment.CurrentDirectory}/../../../../appsettings.Test.json")
            .AddEnvironmentVariables()
            .Build()
            .GetSection("Explorer");

        private static TestConfig TestConfig => Config.Get<TestConfig>();

        public static string GetAircloakApiKeyFromEnvironment()
        {
            if (string.IsNullOrEmpty(Config.Get<TestConfig>().AircloakApiKey))
            {
                throw new Exception("ApiKey needs to be set in environment or in config.");
            }

            return Config.Get<TestConfig>().AircloakApiKey;
        }

        public async Task<HttpResponseMessage> SendExplorerApiRequest(
            HttpMethod method,
            string endpoint,
            object? data,
            string testClassName,
            string vcrSessionName,
            VCRMode vcrMode = VCRMode.Record)
        {
            // For the explorer interactions, most of the times, we don't want to use the cache
            // So we set the default vcr mode to always record.
            var testConfig = GetTestConfig(testClassName, vcrSessionName, vcrMode);

            using var request = new HttpRequestMessage(method, endpoint);
            if (data != null)
            {
                request.Content = new StringContent(JsonSerializer.Serialize(data));
                request.Content.Headers.ContentType = new MediaTypeHeaderValue(System.Net.Mime.MediaTypeNames.Application.Json);
            }

            using var clientHandler = new HttpClientHandler();
            using var cassette = new Cassette(testConfig.VcrCassettePath);
            using var handler = new ReplayingHandler(
                clientHandler,
                testConfig.VcrMode,
                cassette,
                RecordingOptions.RecordAll);

            using var client = CreateDefaultClient(handler);

            return await client.SendAsync(request);
        }

        public new void Dispose()
        {
            Dispose(true);
        }

#pragma warning disable CA1822 // method should be made static
        internal TestConfig GetTestConfig(string testClassName, string vcrSessionName, VCRMode vcrMode = VCRMode.Cache)
        {
            var vcrCassetteFile = new FileInfo($"../../../.vcr/{testClassName}.{vcrSessionName}.yaml");

            // take care to use a small polling interval only when VCR is allowed to playback and we have a non-empty cassette
            // (i.e. when in Record only mode, the polling interval will be large)
            var vcrPlayback = vcrMode == VCRMode.Playback || vcrMode == VCRMode.Cache;
            var pollFrequency = (vcrCassetteFile.Exists && vcrCassetteFile.Length > 0 && vcrPlayback) ?
                    1 :
                    TestConfig.PollFrequency;

            return new TestConfig
            {
                AircloakApiKey = TestConfig.AircloakApiKey,
                DefaultApiUrl = TestConfig.DefaultApiUrl,
                PollFrequency = pollFrequency,
                VcrCassettePath = vcrCassetteFile.FullName,
                VcrMode = vcrMode,
            };
        }
#pragma warning restore CA1822 // method should be made static

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            var testConfig = GetTestConfig(GetType().Name, "WebHost.OutgoingRequests");

            builder.ConfigureServices(services =>
            {
                services
                    .AddAircloakJsonApiServices<ExplorerApiAuthProvider>()
                    .AddHttpMessageHandler(_ => new ReplayingHandler(
                            testConfig.VcrMode,
                            new Cassette(testConfig.VcrCassettePath),
                            RecordingOptions.RecordAll));

                services.Configure<VcrOptions>(Config);
            });
        }
    }
}
