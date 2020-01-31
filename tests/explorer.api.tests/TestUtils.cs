namespace Explorer.Api.Tests
{
    using System;
    using System.Net.Http;
    using Aircloak.JsonApi;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.TestHost;
    using Microsoft.Extensions.Configuration;

    public static class TestUtils
    {
        private static readonly Vcr.VCR VCR = new Vcr.VCR(new Vcr.FileSystemCassetteStorage(
            new System.IO.DirectoryInfo("../../../.vcr")));

        public static string AircloakApiKey => Environment.GetEnvironmentVariable("AIRCLOAK_API_KEY") ??
            throw new InvalidOperationException("Environment variable AIRCLOAK_API_KEY must be set.");

        public static Vcr.RecordMode VcrRecordMode => Enum.Parse<Vcr.RecordMode>(
            Environment.GetEnvironmentVariable("AIRCLOAK_VCR_MODE") ?? "NewEpisodes");

        public static HttpClient CreateHttpClient()
        {
            var config = new ConfigurationBuilder().AddJsonFile("appsettings.Development.json").Build();
            var builder = new WebHostBuilder();
            builder.UseEnvironment("Development");
            builder.UseTestServer();
            builder.ConfigureExplorer(() => VCR.GetVcrHandler());
            builder.UseConfiguration(config);
            using var server = new TestServer(builder);
            return server.CreateClient();
        }

        public static JsonApiSession CreateAircloakApiSession()
        {
            using var httpClient = CreateHttpClient();
            return new JsonApiSession(new JsonApiClient(httpClient), AircloakApiKey);
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