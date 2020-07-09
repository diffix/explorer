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

        private readonly Dictionary<string, VcrSharp.Cassette> cassettes;

        public TestWebAppFactory()
        {
            cassettes = new Dictionary<string, VcrSharp.Cassette>();
        }

        public static string GetAircloakApiKeyFromEnvironment()
        {
            if (string.IsNullOrEmpty(Config.AircloakApiKey))
            {
                throw new Exception("ApiKey needs to be set in environment or in config.");
            }

            return Config.AircloakApiKey;
        }

        public async Task<HttpResponseMessage> SendExplorerApiRequest(HttpMethod method, string endpoint, object? data, string testClassName, string vcrSessionName)
        {
            // For the explorer interactions we never want to use the cache so override the vcr mode.
            // We actually don't need to use the vcr at all but it's useful for debugging...
            // So we set the vcr mode to always record.
            var testConfig = GetTestConfig(testClassName, vcrSessionName, VcrSharp.VCRMode.Record);
            var cassette = LoadCassette(testConfig.VcrCassettePath);

            using var request = new HttpRequestMessage(method, endpoint);
            if (data != null)
            {
                request.Content = new StringContent(JsonSerializer.Serialize(data));
                request.Content.Headers.ContentType = new MediaTypeHeaderValue(System.Net.Mime.MediaTypeNames.Application.Json);
            }

#pragma warning disable CA2000 // call IDisposable.Dispose on handler object
            var handler = new VcrSharp.ReplayingHandler(
                new HttpClientHandler(),
                testConfig.VcrMode,
                cassette,
                VcrSharp.RecordingOptions.RecordAll);
#pragma warning restore CA2000 // call IDisposable.Dispose on handler object

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
                            LoadCassette(testConfig.VcrCassettePath),
                            VcrSharp.RecordingOptions.RecordAll));
            });
        }

        protected override void Dispose(bool disposing)
        {
            foreach (var cassette in cassettes.Values)
            {
                cassette.Dispose();
            }
            base.Dispose(disposing);
        }

        private VcrSharp.Cassette LoadCassette(string path)
        {
            if (!cassettes.ContainsKey(path))
            {
                cassettes[path] = new VcrSharp.Cassette(path);
            }

            return cassettes[path];
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
