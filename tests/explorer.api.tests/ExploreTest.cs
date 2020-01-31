namespace Explorer.Api.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Text.Json;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.TestHost;
    using Microsoft.Extensions.Configuration;
    using Xunit;

    public sealed class ExploreTest
    {
        private static readonly Models.ExploreParams ValidData = new Models.ExploreParams
        {
            ApiKey = Environment.GetEnvironmentVariable("AIRCLOAK_API_KEY") ?? "API_KEY_NOT_SET",
            DataSourceName = "gda_banking",
            TableName = "loans",
            ColumnName = "amount",
        };

        private delegate void ApiTestActionWithContent(HttpResponseMessage response, string content);

        [Fact]
        public void Success()
        {
            TestApi(HttpMethod.Post, "/explore", ValidData, (response, content) =>
                Assert.True(response.IsSuccessStatusCode, content));
        }

        [Fact]
        public void SuccessWithContents()
        {
            TestApi(HttpMethod.Post, "/explore", ValidData, (_, content) =>
            {
                using var jsonContent = JsonDocument.Parse(content);
                var rootEl = jsonContent.RootElement;
                Assert.True(
                    rootEl.ValueKind == JsonValueKind.Object,
                    "Expected a JSON object in the response:\n{content}");
                Assert.True(
                    rootEl.TryGetProperty("id", out var id),
                    $"Expected an 'id' property in:\n{content}");
                Assert.True(
                    rootEl.TryGetProperty("status", out var status),
                    $"Expected a 'status' property in:\n{content}");
                Assert.True(
                    rootEl.TryGetProperty("metrics", out var metrics),
                    $"Expected a 'metrics' property in:\n{content}");
                Assert.True(
                    metrics.ValueKind == JsonValueKind.Array,
                    $"Expected 'metrics' property to contain an array:\n{content}");
                Assert.All<JsonElement>(metrics.EnumerateArray(), el =>
                    Assert.All<string>(new List<string> { "Name", "Type", "Value" }, propName =>
                          Assert.True(
                              el.TryGetProperty(propName, out var _),
                              $"Expected a '{propName}' property in {el}.")));
            });
        }

        [Theory]
        [InlineData("")]
        [InlineData("/")]
        [InlineData("/invalid endpoint test")]
        public void FailWithBadEndPoint(string endpoint)
        {
            TestApi(HttpMethod.Post, endpoint, ValidData, (response, content) =>
                Assert.True(response.StatusCode == HttpStatusCode.NotFound, content));
        }

        [Theory]
        [InlineData("DELETE")]
        [InlineData("GET")]
        [InlineData("HEAD")]
        [InlineData("PUT")]
        public void FailWithBadMethod(string method)
        {
            TestApi(new HttpMethod(method), "/explore", ValidData, (response, content) =>
                Assert.True(response.StatusCode == HttpStatusCode.NotFound, content));
        }

        [Fact]
        public void FailWithEmptyFields()
        {
            var data = new { ApiKey = string.Empty, DataSourceName = string.Empty, TableName = string.Empty, ColumnName = string.Empty };
            TestApi(HttpMethod.Post, "/explore", data, (response, content) =>
            {
                Assert.True(response.StatusCode == HttpStatusCode.BadRequest, content);
                Assert.Contains("The ApiKey field is required.", content, StringComparison.InvariantCulture);
                Assert.Contains("The DataSourceName field is required.", content, StringComparison.InvariantCulture);
                Assert.Contains("The TableName field is required.", content, StringComparison.InvariantCulture);
                Assert.Contains("The ColumnName field is required.", content, StringComparison.InvariantCulture);
            });
        }

        [Fact]
        public void FailWithMissingFields()
        {
            TestApi(HttpMethod.Post, "/explore", new { }, (response, content) =>
            {
                Assert.True(response.StatusCode == HttpStatusCode.BadRequest, content);
                Assert.Contains("The ApiKey field is required.", content, StringComparison.InvariantCulture);
                Assert.Contains("The DataSourceName field is required.", content, StringComparison.InvariantCulture);
                Assert.Contains("The TableName field is required.", content, StringComparison.InvariantCulture);
                Assert.Contains("The ColumnName field is required.", content, StringComparison.InvariantCulture);
            });
        }

        // private IWebHostBuilder CreateWebHostBuilder()
        // {
        //     var builder = new WebHostBuilder();
        //     builder.UseEnvironment("Development");
        //     builder.UseTestServer();
        //     builder.ConfigureExplorer(() => vcr.GetVcrHandler());
        //     return builder;
        // }

        // private async void TestApi(HttpMethod method, string endpoint, object data, ApiTestActionWithContent test)
        // {
        //     // while (!System.Diagnostics.Debugger.IsAttached) { System.Threading.Thread.Sleep(100); }
        //     var configRoot = new ConfigurationBuilder().AddJsonFile("appsettings.Development.json").Build();
        //     var config = configRoot.GetSection("Explorer").Get<ExplorerConfig>();
        //     var vcrMode = Enum.Parse<Vcr.RecordMode>(config.VcrMode ?? "NewEpisodes");
        //     var webHostBuilder = CreateWebHostBuilder().UseConfiguration(configRoot);
        //     using var server = new TestServer(webHostBuilder);
        //     // using var client = server.CreateClient();
        //     using var client = CreateClient();
        //     using var request = new HttpRequestMessage(method, endpoint);
        //     using var vcrCassette = vcr.UseCassette("ExploreTests", ExplorerConfig.VcrMode);
        //     if (data != null)
        //     {
        //         request.Content = new StringContent(JsonSerializer.Serialize(data));
        //         request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        //     }
        //     using var response = await client.SendAsync(request).ConfigureAwait(false);
        //     var responseString = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        //     test(response, responseString);
        // }

        private async void TestApi(HttpMethod method, string endpoint, object data, ApiTestActionWithContent test)
        {
            // TestUtils.WaitDebugger();
            using var vcrCassette = TestUtils.UseVcrCassette("Explore");
            using var client = TestUtils.CreateHttpClient();
            using var request = new HttpRequestMessage(method, endpoint);
            if (data != null)
            {
                request.Content = new StringContent(JsonSerializer.Serialize(data));
                request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            }
            using var response = await client.SendAsync(request).ConfigureAwait(false);
            var responseString = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            test(response, responseString);
        }
    }
}
