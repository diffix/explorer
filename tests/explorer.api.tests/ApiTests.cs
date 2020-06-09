namespace Explorer.Api.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Net.Http;
    using System.Runtime.CompilerServices;
    using System.Text.Json;
    using System.Threading.Tasks;
    using Xunit;

    public sealed class ApiTests : IClassFixture<TestWebAppFactory>
    {
        private static readonly Models.ExploreParams ValidData = new Models.ExploreParams
        {
            ApiUrl = "https://attack.aircloak.com/api/",
            ApiKey = TestWebAppFactory.GetAircloakApiKeyFromEnvironment(),
            DataSourceName = "gda_banking",
            TableName = "loans",
            ColumnName = "amount",
        };

        private readonly TestWebAppFactory factory;

        public ApiTests(TestWebAppFactory factory)
        {
            this.factory = factory;
        }

        private delegate void ApiTestActionWithContent(HttpResponseMessage response, string content);

        private delegate T ApiTestActionWithContent<T>(HttpResponseMessage response, string content);

        [Fact]
        public async Task Success()
        {
            await TestApi(HttpMethod.Post, "/explore", ValidData, (response, _) =>
                Assert.True(response.IsSuccessStatusCode, $"Response code {response.StatusCode}."));
        }

        [Fact]
        public async Task SuccessWithContents()
        {
            await TestApi(HttpMethod.Post, "/explore", ValidData, (response, content) =>
            {
                Assert.True(response.IsSuccessStatusCode, $"Response code {response.StatusCode}.");

                using var jsonContent = JsonDocument.Parse(content);
                var rootEl = jsonContent.RootElement;
                Assert.True(
                    rootEl.ValueKind == JsonValueKind.Object,
                    $"Expected a JSON object in the response:\n{content}");
                Assert.True(
                    rootEl.TryGetProperty("id", out var id),
                    $"Expected an 'id' property in:\n{content}");
                Assert.True(
                    rootEl.TryGetProperty("status", out var status),
                    $"Expected a 'status' property in:\n{content}");
            });
        }

        [Fact]
        public async Task SuccessWithResult()
        {
            var explorerGuid = await TestApi(HttpMethod.Post, "/explore", ValidData, (response, content) =>
            {
                Assert.True(response.IsSuccessStatusCode, $"Response code {response.StatusCode}.");

                using var jsonContent = JsonDocument.Parse(content);
                var rootEl = jsonContent.RootElement;
                Assert.True(
                    rootEl.ValueKind == JsonValueKind.Object,
                    $"Expected a JSON object in the response:\n{content}");
                Assert.True(
                    rootEl.TryGetProperty("id", out var id),
                    $"Expected an 'id' property in:\n{content}");
                Assert.True(
                    rootEl.TryGetProperty("status", out var status),
                    $"Expected a 'status' property in:\n{content}");

                Assert.True(id.TryGetGuid(out var explorerGuid));

                return explorerGuid;
            });

            await TestApi(HttpMethod.Get, $"/result/{explorerGuid}", null, (response, content) =>
            {
                Assert.True(response.IsSuccessStatusCode, $"Response code {response.StatusCode}.");

                using var jsonContent = JsonDocument.Parse(content);
                var rootEl = jsonContent.RootElement;
                Assert.True(
                    rootEl.ValueKind == JsonValueKind.Object,
                    $"Expected a JSON object in the response:\n{content}");
                Assert.True(
                    rootEl.TryGetProperty("id", out var id),
                    $"Expected an 'id' property in:\n{content}");
                Assert.True(id.GetGuid() == explorerGuid);
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
                    Assert.All<string>(new List<string> { "name", "value" }, propName =>
                          Assert.True(
                              el.TryGetProperty(propName, out var _),
                              $"Expected a '{propName}' property in {el}.")));
            });
        }

        [Theory]
        [InlineData("")]
        [InlineData("/")]
        [InlineData("/invalid endpoint test")]
        public async Task FailWithBadEndPoint(string endpoint)
        {
            await TestApi(HttpMethod.Post, endpoint, ValidData, test: (response, content) =>
                Assert.True(response.StatusCode == HttpStatusCode.NotFound, content));
        }

        [Theory]
        [InlineData("DELETE")]
        [InlineData("GET")]
        [InlineData("HEAD")]
        [InlineData("PUT")]
        public async Task FailWithBadMethod(string method)
        {
            await TestApi(new HttpMethod(method), "/explore", ValidData, test: (response, content) =>
                Assert.True(response.StatusCode == HttpStatusCode.NotFound, content));
        }

        [Fact]
        public async Task FailWithEmptyFields()
        {
            var data = new
            {
                ApiKey = string.Empty,
                DataSourceName = string.Empty,
                TableName = string.Empty,
                ColumnName = string.Empty,
            };

            await TestApi(HttpMethod.Post, "/explore", data, test: (response, content) =>
            {
                Assert.True(response.StatusCode == HttpStatusCode.BadRequest, content);
                Assert.Contains("The ApiKey field is required.", content, StringComparison.InvariantCulture);
                Assert.Contains("The DataSourceName field is required.", content, StringComparison.InvariantCulture);
                Assert.Contains("The TableName field is required.", content, StringComparison.InvariantCulture);
                Assert.Contains("The ColumnName field is required.", content, StringComparison.InvariantCulture);
            });
        }

        [Fact]
        public async Task FailWithMissingFields()
        {
            await TestApi(HttpMethod.Post, "/explore", new { }, test: (response, content) =>
            {
                Assert.True(response.StatusCode == HttpStatusCode.BadRequest, content);
                Assert.Contains("The ApiKey field is required.", content, StringComparison.InvariantCulture);
                Assert.Contains("The DataSourceName field is required.", content, StringComparison.InvariantCulture);
                Assert.Contains("The TableName field is required.", content, StringComparison.InvariantCulture);
                Assert.Contains("The ColumnName field is required.", content, StringComparison.InvariantCulture);
            });
        }

        [Fact]
        public async Task FailWithInvalidApiKey()
        {
            var invalidData = new Models.ExploreParams
            {
                ApiKey = "INVALID_KEY",
                ApiUrl = ValidData.ApiUrl,
                DataSourceName = ValidData.DataSourceName,
                TableName = ValidData.TableName,
                ColumnName = ValidData.ColumnName,
            };

            await TestApi(
                HttpMethod.Post,
                "/explore",
                data: invalidData,
                test: (response, content) =>
                {

                    Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
                    Assert.Contains("Unauthorized", content, StringComparison.InvariantCultureIgnoreCase);
                });
        }

        private async Task TestApi(
            HttpMethod method,
            string endpoint,
            object? data,
            ApiTestActionWithContent test,
            [CallerMemberName] string vcrSessionName = "")
        {
            using var response = await factory.SendExplorerApiRequest(method, endpoint, data, nameof(ApiTests), vcrSessionName);
            var responseString = await response.Content.ReadAsStringAsync();
            test(response, responseString);
        }

        private async Task<T> TestApi<T>(
            HttpMethod method,
            string endpoint,
            object? data,
            ApiTestActionWithContent<T> test,
            [CallerMemberName] string vcrSessionName = "")
        {
            using var response = await factory.SendExplorerApiRequest(method, endpoint, data, nameof(ApiTests), vcrSessionName);
            var responseString = await response.Content.ReadAsStringAsync();
            return test(response, responseString);
        }
    }
}
