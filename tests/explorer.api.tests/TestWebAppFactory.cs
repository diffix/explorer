#pragma warning disable CA1822 // make method static
namespace Explorer.Api.Tests
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Text.Json;
    using Explorer.Api;
    using Microsoft.AspNetCore.Mvc.Testing;
    using Microsoft.Extensions.Configuration;

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
            var vcrCassetteInfo = GetVcrCasetteInfo(testClassName, vcrSessionName);

            var handler = new VcrSharp.ReplayingHandler(
                LoadCassette(vcrCassetteInfo.FullName),
                VcrSharp.RecordingOptions.RecordAll);

            // Override the vcr mode to always record.
            handler.CurrentVCRMode = VcrSharp.VCRMode.Record;

            return CreateDefaultClient(handler);
        }

#pragma warning disable CA2000 // call IDisposable.Dispose on handler object
        public HttpClient CreateAircloakApiHttpClient(FileInfo vcrCassetteInfo, bool expectFail = false)
        {
            var vcrOptions = expectFail
                            ? VcrSharp.RecordingOptions.FailureOnly
                            : VcrSharp.RecordingOptions.SuccessOnly;

            var vcrHandler = new VcrSharp.ReplayingHandler(LoadCassette(vcrCassetteInfo.FullName), vcrOptions);

            var variableName = Config.ApiKeyEnvironmentVariable ??
                throw new Exception("ApiKeyEnvironmentVariable config item is missing.");

            var authenticatingVcrHandler = Aircloak.JsonApi.StaticApiKeyAuthHandler.FromEnvironmentVariable(
                variableName,
                innerHandler: vcrHandler);

            var client = new HttpClient(authenticatingVcrHandler, true) { BaseAddress = Config.AircloakApiUrl };

            return client;
        }
#pragma warning restore CA2000 // call IDisposable.Dispose on handler object

        public FileInfo GetVcrCasetteInfo(string testClassName, string vcrSessionName)
        {
            return new FileInfo($"../../../.vcr/{testClassName}.{vcrSessionName}.yaml");
        }

        public TimeSpan? GetApiPollingFrequency(FileInfo vcrCassetteInfo)
        {
            return (vcrCassetteInfo.Exists && vcrCassetteInfo.Length > 0) ? TimeSpan.FromMilliseconds(1) : default(TimeSpan?);
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
