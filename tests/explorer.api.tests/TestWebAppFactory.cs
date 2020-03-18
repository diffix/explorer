#pragma warning disable CA1822 // make method static
namespace Explorer.Api.Tests
{
    using Aircloak.JsonApi;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Text.Json;
    using Explorer.Api;
    using Explorer.Api.Authentication;
    using Microsoft.AspNetCore.Mvc.Testing;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;

    public class TestWebAppFactory : WebApplicationFactory<Startup>
    {
        public static readonly ExplorerConfig Config = new ConfigurationBuilder()
            .AddJsonFile("appsettings.Development.json")
            .Build()
            .GetSection("Explorer")
            .Get<ExplorerConfig>();

        private readonly Dictionary<string, VcrSharp.Cassette> cassettes;

        public TestWebAppFactory()
        {
            cassettes = new Dictionary<string, VcrSharp.Cassette>();
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

        protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
        {
            var testConfig = GetTestConfig(GetType().ToString(), "WebHost.OutgoingRequests");

            builder.ConfigureServices(services =>
            {
                services
                    .AddAircloakJsonApiServices<ExplorerApiAuthProvider>(Config.AircloakApiUrl ??
                        throw new Exception("No Aircloak Api base Url provided in Explorer config."))
                    .AddHttpMessageHandler(_ => new VcrSharp.ReplayingHandler(
                            testConfig.VcrMode,
                            LoadCassette(testConfig.VcrCassettePath),
                            VcrSharp.RecordingOptions.RecordAll));
            });
        }

        public HttpRequestMessage CreateHttpRequest(HttpMethod method, string endpoint, object? data)
        {
            var request = new HttpRequestMessage(method, endpoint);
            if (data != null)
            {
                request.Content = new StringContent(JsonSerializer.Serialize(data));
                request.Content.Headers.ContentType = new MediaTypeHeaderValue(System.Net.Mime.MediaTypeNames.Application.Json);
            }
            return request;
        }

        public HttpClient CreateExplorerApiHttpClient(string testClassName, string vcrSessionName)
        {
            // For the explorer interactions we never want to use the cache so override the vcr mode.
            // We actually don't need to use the vcr at all but it's useful for debugging...
            // So we set the vcr mode to always record.
            var testConfig = GetTestConfig(testClassName, vcrSessionName, VcrSharp.VCRMode.Record);
            var cassette = LoadCassette(testConfig.VcrCassettePath);

            var handler = new VcrSharp.ReplayingHandler(
                new HttpClientHandler(),
                testConfig.VcrMode,
                cassette,
                VcrSharp.RecordingOptions.RecordAll);

            return CreateDefaultClient(handler);
        }

#pragma warning disable CA2000 // call IDisposable.Dispose on handler object
        public JsonApiClient CreateJsonApiClient(string vcrCassettePath, bool expectFail = false)
        {
            var vcrOptions = expectFail ? VcrSharp.RecordingOptions.FailureOnly : VcrSharp.RecordingOptions.SuccessOnly;
            var vcrCassette = LoadCassette(vcrCassettePath);
            var vcrHandler = new VcrSharp.ReplayingHandler(new HttpClientHandler(), VcrSharp.VCRMode.Cache, vcrCassette, vcrOptions);
            var httpClient = new HttpClient(vcrHandler, true) { BaseAddress = Config.AircloakApiUrl };
            var authProvider = EnvironmentVariableAuthProvider();
            return new JsonApiClient(httpClient, authProvider);
        }
#pragma warning restore CA2000 // call IDisposable.Dispose on handler object

        public IAircloakAuthenticationProvider EnvironmentVariableAuthProvider()
        {
            var variableName = Config.ApiKeyEnvironmentVariable ??
                throw new Exception("ApiKeyEnvironmentVariable config item is missing.");

            return StaticApiKeyAuthProvider.FromEnvironmentVariable(variableName);
        }

        public static string GetAircloakApiKeyFromEnvironment()
        {
            var variableName = Config.ApiKeyEnvironmentVariable ??
                throw new Exception("ApiKeyEnvironmentVariable config item is missing.");

            var apiKey = Environment.GetEnvironmentVariable(variableName) ??
                throw new Exception($"Environment variable {variableName} not set.");

            return apiKey;
        }

        public new void Dispose()
        {
            Dispose(true);
        }

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
    }
}
#pragma warning restore CA1822 // make method static
