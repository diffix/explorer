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
            DataSource = "gda_banking",
            Table = "loans",
            Columns = new List<string> { "amount" },
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
            var data = new Models.ExploreParams
            {
                ApiKey = ValidData.ApiKey,
                ApiUrl = ValidData.ApiUrl,
                DataSource = "gda_banking",
                Table = "loans",
                Columns = new[] { "amount", "firstname" },
            };
            var testConfig = factory.GetTestConfig(nameof(ApiTests), nameof(SuccessWithResult));

            var explorerGuid = await TestApi(HttpMethod.Post, "/explore", data, (response, content) =>
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
                Assert.True(id.TryGetGuid(out var explorerGuid));

                Assert.True(
                    rootEl.TryGetProperty("status", out var status),
                    $"Expected a 'status' property in:\n{content}");

                Assert.True(
                    rootEl.TryGetProperty("columns", out var columns),
                    $"Expected a 'columns' property in:\n{content}");

                Assert.True(
                    columns.ValueKind == JsonValueKind.Array,
                    $"Expected 'columns' property to contain an array:\n{content}");

                return explorerGuid;
            });

            await TestExploreResult(HttpMethod.Get, explorerGuid, testConfig.PollFrequency, (response, content) =>
            {
                Assert.True(response.IsSuccessStatusCode, $"Response code {response.StatusCode}.");

                using var jsonContent = JsonDocument.Parse(content);
                var rootEl = jsonContent.RootElement;
                Assert.True(
                    rootEl.TryGetProperty("status", out var status),
                    $"Expected a 'status' property in:\n{content}");

                Assert.True(
                    rootEl.TryGetProperty("columns", out var columns),
                    $"Expected a 'columns' property in:\n{content}");
                Assert.True(
                    columns.ValueKind == JsonValueKind.Array,
                    $"Expected 'columns' property to contain an array:\n{content}");

                Assert.True(
                    rootEl.TryGetProperty("sampleData", out var sampleData),
                    $"Expected a 'sampleData' property in:\n{content}");
                Assert.True(
                    sampleData.ValueKind == JsonValueKind.Array,
                    $"Expected 'sampleData' property to contain an array:\n{content}");

                if (status.GetString() == "Complete")
                {
                    Assert.Equal(data.Columns.Count, columns.GetArrayLength());
                    foreach (var item in columns.EnumerateArray())
                    {
                        Assert.True(item.TryGetProperty("column", out var column));
                        Assert.Contains(column.GetString(), data.Columns);

                        Assert.True(item.TryGetProperty("metrics", out var metrics));
                        Assert.True(metrics.GetArrayLength() > 0, $"Metrics for column {column} are empty!");
                        Assert.All(metrics.EnumerateArray(), el =>
                            Assert.All(new List<string> { "name", "value" }, propName =>
                                Assert.True(
                                    el.TryGetProperty(propName, out var _),
                                    $"Expected a '{propName}' property in {el}.")));
                    }

                    Assert.True(sampleData.GetArrayLength() > 0, "SampleData is empty!");
                    foreach (var row in sampleData.EnumerateArray())
                    {
                        Assert.True(
                            row.ValueKind == JsonValueKind.Array,
                            $"Expected 'sampleData' property to contain array elements:\n{content}");
                        Assert.Equal(data.Columns.Count, row.GetArrayLength());
                    }
                }
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
                DataSource = string.Empty,
                Table = string.Empty,
                Columns = new List<string>(),
            };

            await TestApi(HttpMethod.Post, "/explore", data, test: (response, content) =>
            {
                Assert.True(response.StatusCode == HttpStatusCode.BadRequest, content);
                Assert.Contains("The ApiUrl field is required.", content, StringComparison.InvariantCulture);
                Assert.Contains("The ApiKey field is required.", content, StringComparison.InvariantCulture);
                Assert.Contains("The DataSource field is required.", content, StringComparison.InvariantCulture);
                Assert.Contains("The Table field is required.", content, StringComparison.InvariantCulture);
            });
        }

        [Fact]
        public async Task FailWithMissingFields()
        {
            await TestApi(HttpMethod.Post, "/explore", new { }, test: (response, content) =>
            {
                Assert.True(response.StatusCode == HttpStatusCode.BadRequest, content);
                Assert.Contains("The ApiUrl field is required.", content, StringComparison.InvariantCulture);
                Assert.Contains("The ApiKey field is required.", content, StringComparison.InvariantCulture);
                Assert.Contains("The DataSource field is required.", content, StringComparison.InvariantCulture);
                Assert.Contains("The Table field is required.", content, StringComparison.InvariantCulture);
            });
        }

        [Fact]
        public async Task FailWithInvalidApiKey()
        {
            var invalidData = new Models.ExploreParams
            {
                ApiKey = "INVALID_KEY",
                ApiUrl = ValidData.ApiUrl,
                DataSource = ValidData.DataSource,
                Table = ValidData.Table,
                Columns = ValidData.Columns,
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

        private async Task TestExploreResult(
            HttpMethod method,
            Guid explorerGuid,
            TimeSpan pollFrequency,
            ApiTestActionWithContent test,
            [CallerMemberName] string vcrSessionName = "")
        {
            while (true)
            {
                using var response = await factory.SendExplorerApiRequest(method, $"/result/{explorerGuid}", null, nameof(ApiTests), vcrSessionName);
                var content = await response.Content.ReadAsStringAsync();
                using var jsonContent = JsonDocument.Parse(content);
                var rootEl = jsonContent.RootElement;
                Assert.True(
                    rootEl.ValueKind == JsonValueKind.Object,
                    $"Expected a JSON object in the response:\n{content}");

                Assert.True(
                    rootEl.TryGetProperty("status", out var status),
                    $"Expected a 'status' property in:\n{content}");

                Assert.True(
                    rootEl.TryGetProperty("id", out var id),
                    $"Expected an 'id' property in:\n{content}");
                Assert.True(id.GetGuid() == explorerGuid);

                test(response, content);
                if (status.GetString() == "Complete")
                {
                    break;
                }
                await Task.Delay(pollFrequency);
            }
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
