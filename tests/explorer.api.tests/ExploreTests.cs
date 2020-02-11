namespace Explorer.Api.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Net.Http;
    using System.Text.Json;
    using Xunit;

    public sealed class ExploreTests : IClassFixture<TestWebAppFactory>
    {
        private static readonly Models.ExploreParams ValidData = new Models.ExploreParams
        {
            ApiKey = TestWebAppFactory.Config.AircloakApiKey,
            DataSourceName = "gda_banking",
            TableName = "loans",
            ColumnName = "amount",
        };

        private readonly TestWebAppFactory factory;

        public ExploreTests(TestWebAppFactory factory)
        {
            this.factory = factory;
        }

        private delegate void ApiTestActionWithContent(HttpResponseMessage response, string content);

        [Fact]
        public void Success()
        {
            TestApi(nameof(Success), HttpMethod.Post, "/explore", ValidData, (response, content) =>
                Assert.True(response.IsSuccessStatusCode, content));
        }

        [Fact]
        public void SuccessWithContents()
        {
            TestApi(nameof(SuccessWithContents), HttpMethod.Post, "/explore", ValidData, (_, content) =>
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
            TestApi(nameof(FailWithBadEndPoint), HttpMethod.Post, endpoint, ValidData, (response, content) =>
                Assert.True(response.StatusCode == HttpStatusCode.NotFound, content));
        }

        [Theory]
        [InlineData("DELETE")]
        [InlineData("GET")]
        [InlineData("HEAD")]
        [InlineData("PUT")]
        public void FailWithBadMethod(string method)
        {
            TestApi(nameof(FailWithBadMethod), new HttpMethod(method), "/explore", ValidData, (response, content) =>
                Assert.True(response.StatusCode == HttpStatusCode.NotFound, content));
        }

        [Fact]
        public void FailWithEmptyFields()
        {
            var data = new { ApiKey = string.Empty, DataSourceName = string.Empty, TableName = string.Empty, ColumnName = string.Empty };
            TestApi(nameof(FailWithEmptyFields), HttpMethod.Post, "/explore", data, (response, content) =>
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
            TestApi(nameof(FailWithMissingFields), HttpMethod.Post, "/explore", new { }, (response, content) =>
            {
                Assert.True(response.StatusCode == HttpStatusCode.BadRequest, content);
                Assert.Contains("The ApiKey field is required.", content, StringComparison.InvariantCulture);
                Assert.Contains("The DataSourceName field is required.", content, StringComparison.InvariantCulture);
                Assert.Contains("The TableName field is required.", content, StringComparison.InvariantCulture);
                Assert.Contains("The ColumnName field is required.", content, StringComparison.InvariantCulture);
            });
        }

        private async void TestApi(string vcrSessionName, HttpMethod method, string endpoint, object data, ApiTestActionWithContent test)
        {
            // TestUtils.WaitDebugger();
            using var client = factory.CreateExplorerApiHttpClient(nameof(ExploreTests), vcrSessionName);
            using var request = factory.CreateHttpRequest(method, endpoint, data);
            using var response = await client.SendAsync(request);
            var responseString = await response.Content.ReadAsStringAsync();
            test(response, responseString);
        }
    }
}
