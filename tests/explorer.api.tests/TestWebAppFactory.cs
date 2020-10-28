namespace Explorer.Api.Tests
{
    using System;
    using System.Collections.Generic;
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

    public class TestWebAppFactory : WebApplicationFactory<Startup>
    {
        public static readonly ExplorerConfig Config = new ConfigurationBuilder()
            .AddJsonFile($"{Environment.CurrentDirectory}/../../../../appsettings.Test.json")
            .AddEnvironmentVariables()
            .Build()
            .GetSection("Explorer")
            .Get<ExplorerConfig>();

        public static string GetAircloakApiKeyFromEnvironment()
        {
            if (string.IsNullOrEmpty(Config.AircloakApiKey))
            {
                throw new Exception("ApiKey needs to be set in environment or in config.");
            }

            return Config.AircloakApiKey;
        }

        public async Task<HttpResponseMessage> SendExplorerApiRequest(
            HttpMethod method,
            string endpoint,
            object? data,
            string testClassName,
            string vcrSessionName,
            VcrSharp.VCRMode vcrMode = VcrSharp.VCRMode.Record)
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
            using var cassette = new VcrSharp.Cassette(testConfig.VcrCassettePath);
            using var handler = new VcrSharp.ReplayingHandler(
                clientHandler,
                testConfig.VcrMode,
                cassette,
                VcrSharp.RecordingOptions.RecordAll);

            using var client = CreateDefaultClient(handler);

            return await client.SendAsync(request);
        }

        public new void Dispose()
        {
            Dispose(true);
        }

#pragma warning disable CA1822 // method should be made static
        internal TestConfig GetTestConfig(string testClassName, string vcrSessionName, VcrSharp.VCRMode vcrMode = VcrSharp.VCRMode.Cache)
        {
            var vcrCassette = new FileInfo($"../../../.vcr/{testClassName}.{vcrSessionName}.yaml");

            // take care to use a small polling interval only when VCR is allowed to playback and we have a non-empty cassette
            // (i.e. when in Record only mode, the polling interval will be large)
            var vcrPlayback = vcrMode == VcrSharp.VCRMode.Playback || vcrMode == VcrSharp.VCRMode.Cache;
            var pollFrequency = (vcrCassette.Exists && vcrCassette.Length > 0 && vcrPlayback) ?
                    TimeSpan.FromMilliseconds(1) :
                    Config.PollFrequencyTimeSpan;

            return new TestConfig(vcrCassette.FullName, pollFrequency, vcrMode);
        }
#pragma warning restore CA1822 // method should be made static

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            var testConfig = GetTestConfig(GetType().Name, "WebHost.OutgoingRequests");

            builder.ConfigureServices(services =>
            {
                services
                    .AddAircloakJsonApiServices<ExplorerApiAuthProvider>()
                    .AddHttpMessageHandler(_ => new VcrSharp.ReplayingHandler(
                            testConfig.VcrMode,
                            new VcrSharp.Cassette(testConfig.VcrCassettePath),
                            VcrSharp.RecordingOptions.RecordAll));
            });
        }

        internal class TestConfig
        {
            public TestConfig(string vcrCassettePath, TimeSpan pollFrequency, VcrSharp.VCRMode vcrMode)
            {
                VcrCassettePath = vcrCassettePath;
                PollFrequency = pollFrequency;
                VcrMode = vcrMode;
            }

            public string VcrCassettePath { get; }

            public TimeSpan PollFrequency { get; }

            public VcrSharp.VCRMode VcrMode { get; }
        }
    }
}
