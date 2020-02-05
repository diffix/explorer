namespace Explorer.Api.Tests
{
    using System;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Text.Json;
    using Aircloak.JsonApi;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.TestHost;
    using Microsoft.Extensions.Configuration;

    public static class TestUtils
    {
        public static readonly string AircloakApiKey;

        public static readonly Vcr.RecordMode VcrRecordMode;

        public static readonly TestServer TestServer;

        public static readonly JsonApiSession JsonApiSession;

        public static readonly HttpClient HttpClient;

        private static readonly HttpClient JsonApiHttpClient;

        private static readonly Vcr.VCR VCR;

#pragma warning disable CA1810 // remove explicit static constructor
#pragma warning disable CA1065 // do not raise exception in static constructor
        static TestUtils()
        {
            AircloakApiKey = Environment.GetEnvironmentVariable("AIRCLOAK_API_KEY") ??
                throw new InvalidOperationException("Environment variable AIRCLOAK_API_KEY must be set.");

            VcrRecordMode = Enum.Parse<Vcr.RecordMode>(Environment.GetEnvironmentVariable("AIRCLOAK_VCR_MODE") ?? "NewEpisodes");

            VCR = new Vcr.VCR(new Vcr.FileSystemCassetteStorage(new System.IO.DirectoryInfo("../../../.vcr")));

            var configRoot = new ConfigurationBuilder().AddJsonFile("appsettings.Development.json").Build();
            var config = configRoot.GetSection("Explorer").Get<ExplorerConfig>();

            var builder = new WebHostBuilder();
            builder.UseEnvironment("Development");
            builder.UseTestServer();
            // builder.ConfigureExplorer(() => VCR.GetVcrHandler());
            builder.ConfigureExplorer(null);
            builder.UseConfiguration(configRoot);
            TestServer = new TestServer(builder);

            HttpClient = TestServer.CreateClient();

            JsonApiHttpClient = new HttpClient();
            JsonApiHttpClient.BaseAddress = config.AircloakApiUrl;
            JsonApiHttpClient.DefaultRequestHeaders.TryAddWithoutValidation("auth-token", AircloakApiKey);
            JsonApiSession = new JsonApiSession(JsonApiHttpClient);
        }
#pragma warning restore CA1810 // remove explicit static constructor
#pragma warning restore CA1065 // do not raise exception in static constructor

        public static HttpRequestMessage CreateHttpRequest(HttpMethod method, string endpoint, object data)
        {
            var request = new HttpRequestMessage(method, endpoint);
            if (data != null)
            {
                request.Content = new StringContent(JsonSerializer.Serialize(data));
                request.Content.Headers.ContentType = new MediaTypeHeaderValue(System.Net.Mime.MediaTypeNames.Application.Json);
            }
            return request;
        }

        public static Vcr.Cassette UseVcrCassette(string name)
        {
            return VCR.UseCassette(name, VcrRecordMode);
        }

        public static void WaitDebugger()
        {
            while (!System.Diagnostics.Debugger.IsAttached)
            {
                System.Threading.Thread.Sleep(300);
            }
        }
    }
}