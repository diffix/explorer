namespace Explorer.Api.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Runtime.CompilerServices;
    using System.Text.Json;
    using System.Threading.Tasks;

    using Explorer.Api.Models;
    using Newtonsoft.Json.Linq;
    using Newtonsoft.Json.Schema;
    using Xunit;
    using YamlDotNet.Serialization;

    public sealed class ApiTests : IClassFixture<TestWebAppFactory>
    {
        private const string ApiRoot = "api/v1";
        private static readonly string ExploreEndpoint = $"{ApiRoot}/explore";
        private static readonly string ResultEndpoint = $"{ApiRoot}/result";

        private static readonly ExploreParams ValidData = new ExploreParams
        {
            ApiUrl = "https://attack.aircloak.com/api/",
            ApiKey = TestWebAppFactory.GetAircloakApiKeyFromEnvironment(),
            DataSource = "gda_banking",
            Table = "loans",
            Columns = ImmutableArray.Create("amount"),
        };

        private readonly TestWebAppFactory factory;
        private readonly JSchema explorerSchema;

        public ApiTests(TestWebAppFactory factory)
        {
            this.factory = factory;

            using var schemaReader = File.OpenText("../../../../../explorer.schema.yaml");
            var deserializer = new DeserializerBuilder().Build();
            var yamlSchema = deserializer.Deserialize(schemaReader);
            if (yamlSchema == null)
            {
                throw new InvalidDataException("YAML explorer schema is invalid.");
            }

            using var jsonSchemaWriter = new StringWriter();
            Newtonsoft.Json.JsonSerializer
                .Create(new Newtonsoft.Json.JsonSerializerSettings { Formatting = Newtonsoft.Json.Formatting.Indented })
                .Serialize(jsonSchemaWriter, yamlSchema);
            var jsonSchema = jsonSchemaWriter.ToString();
            explorerSchema = JSchema.Parse(jsonSchema);

            // for debugging the JSON schema
            // File.WriteAllText("../../../../../explorer.schema.json", jsonSchema);
            // explorerSchema = JSchema.Parse(File.ReadAllText("../../../../../explorer.schema.json"));
        }

        private delegate void ApiTestActionWithContent(HttpResponseMessage response, string content);

        private delegate T ApiTestActionWithContent<T>(HttpResponseMessage response, string content);

        [Fact]
        public async Task Success()
        {
            await TestApi(HttpMethod.Post, ExploreEndpoint, ValidData, (response, _) =>
                Assert.True(response.IsSuccessStatusCode, $"Response code {response.StatusCode}."));
        }

        [Fact]
        public async Task SuccessWithContents()
        {
            await TestApi(HttpMethod.Post, ExploreEndpoint, ValidData, (response, content) =>
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
                Assert.True(
                    rootEl.TryGetProperty("versionInfo", out var versionInfo),
                    $"Expected a 'versionInfo' property in:\n{content}");
            });
        }

        [Fact]
        public async Task SuccessWithResultNoColumns() => await SuccessWithResult(
            new ExploreParams { DataSource = "gda_banking", Table = "loans", Columns = ImmutableArray<string>.Empty, });

        [Fact]
        public async Task SuccessWithResultIntCategorical() => await SuccessWithResult(
            new ExploreParams { DataSource = "gda_banking", Table = "loans", Columns = ImmutableArray.Create("duration"), });

        [Fact]
        public async Task SuccessWithResultIntNonCategorical() => await SuccessWithResult(
            new ExploreParams { DataSource = "gda_banking", Table = "loans", Columns = ImmutableArray.Create("amount"), });

        [Fact]
        public async Task SuccessWithResultIntRealCategorical() => await SuccessWithResult(
            new ExploreParams { DataSource = "gda_banking", Table = "loans", Columns = ImmutableArray.Create("payments"), });

        [Fact]
        public async Task SuccessWithResultTextCategorical() => await SuccessWithResult(
            new ExploreParams { DataSource = "gda_banking", Table = "loans", Columns = ImmutableArray.Create("status"), });

        [Fact]
        public async Task SuccessWithResultTextNonCategorical() => await SuccessWithResult(
            new ExploreParams { DataSource = "gda_banking", Table = "loans", Columns = ImmutableArray.Create("firstname"), });

        [Fact]
        public async Task SuccessWithResultDate() => await SuccessWithResult(
            new ExploreParams { DataSource = "gda_taxi", Table = "rides", Columns = ImmutableArray.Create("birthdate"), });

        [Fact]
        public async Task SuccessWithResultDatetime() => await SuccessWithResult(
            new ExploreParams { DataSource = "gda_taxi", Table = "rides", Columns = ImmutableArray.Create("pickup_datetime") });

        [Fact]
        public async Task SuccessWithResultMultiColumns() => await SuccessWithResult(
            new ExploreParams { DataSource = "cov_clear", Table = "survey", Columns = ImmutableArray.Create("test_date", "fever", "how_anxious", "lat", "email") });

        public async Task SuccessWithResult(ExploreParams pdata, [CallerMemberName] string vcrSessionName = "")
        {
            var data = new ExploreParams
            {
                ApiKey = ValidData.ApiKey,
                ApiUrl = ValidData.ApiUrl,
                DataSource = pdata.DataSource,
                Table = pdata.Table,
                Columns = pdata.Columns,
            };

            // The vcr cache has to be cleared when there are schema changes.
            var explorerGuid = await TestApi(
                HttpMethod.Post,
                ExploreEndpoint,
                data,
                ReadExplorationGuid,
                vcrSessionName,
                VcrSharp.VCRMode.Cache);

            var testConfig = factory.GetTestConfig(nameof(ApiTests), vcrSessionName);
            await TestExploreResult(
                HttpMethod.Get,
                explorerGuid,
                testConfig.PollFrequency,
                CheckExploreResult,
                vcrSessionName,
                VcrSharp.VCRMode.Cache);

            Guid ReadExplorationGuid(HttpResponseMessage response, string content)
            {
                Assert.True(response.IsSuccessStatusCode, $"Response code {response.StatusCode}.");

                var jsonContent = JObject.Parse(content);
                var errorMessages = new List<string>() as IList<string>;
                var isValidContent = jsonContent.IsValid(explorerSchema, out errorMessages);
                Assert.True(isValidContent, string.Join('\n', errorMessages));

                Assert.True(
                    jsonContent.Type == JTokenType.Object,
                    $"Expected a JSON object in the response:\n{content}");

                Assert.True(
                    jsonContent.TryGetValue("id", out var id),
                    $"Expected an 'id' property in:\n{content}");
                Assert.True(Guid.TryParse(id.Value<string>(), out var explorerGuid));

                return explorerGuid;
            }

            void CheckExploreResult(HttpResponseMessage response, string content)
            {
                Assert.True(response.IsSuccessStatusCode, $"Response code {response.StatusCode}.");

                var jsonContent = JObject.Parse(content);
                var errorMessages = new List<string>() as IList<string>;
                var isValidContent = jsonContent.IsValid(explorerSchema, out errorMessages);
                Assert.True(isValidContent, string.Join('\n', errorMessages));

                Assert.Contains("status", jsonContent);
                var status = (string)jsonContent["status"];

                Assert.Contains("columns", jsonContent);
                var columns = (JArray)jsonContent["columns"];
                Assert.NotNull(columns);

                Assert.Contains("sampleData", jsonContent);
                var sampleData = (JArray)jsonContent["sampleData"];
                Assert.NotNull(sampleData);

                if (status == "Complete")
                {
                    Assert.Equal(data.Columns.Length, columns.Count);
                    foreach (var jitem in columns)
                    {
                        Assert.Equal(JTokenType.Object, jitem.Type);
                        var item = (JObject)jitem;
                        Assert.True(item.ContainsKey("column"));
                        var column = (string)item["column"];
                        Assert.Contains(column, data.Columns);

                        Assert.True(item.ContainsKey("metrics"));
                        var metrics = (JArray)item["metrics"];
                        Assert.True(metrics.Count > 0, $"Metrics for column {column} are empty!");
                        Assert.All(metrics, jmetric =>
                        {
                            Assert.Equal(JTokenType.Object, jmetric.Type);
                            var metric = (JObject)jmetric;
                            Assert.True(metric.ContainsKey("name"));
                            Assert.True(metric.ContainsKey("value"));
                        });
                    }

                    if (data.Columns.Length > 0)
                    {
                        Assert.True(sampleData.Count > 0, "SampleData is empty!");
                        foreach (var row in sampleData)
                        {
                            Assert.True(
                                row.Type == JTokenType.Array,
                                $"Expected 'sampleData' property to contain array elements:\n{content}");
                            Assert.Equal(data.Columns.Length, row.Count());
                        }
                    }
                }
            }
        }

        [Theory]
        [InlineData("")]
        [InlineData("/")]
        [InlineData("/invalid endpoint test")]
        public async Task FailWithBadEndPoint(string endpoint)
        {
            var apiEndpoint = ApiRoot + endpoint;

            await TestApi(HttpMethod.Post, apiEndpoint, ValidData, test: (response, content) =>
            {
                Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
                var jsonContent = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(content);
                Assert.True(jsonContent.ContainsKey("error"), $"Expected an 'error' object in payload: {content}");
            });
        }

        [Theory]
        [InlineData("DELETE")]
        [InlineData("GET")]
        [InlineData("HEAD")]
        [InlineData("PUT")]
        public async Task FailWithBadMethod(string method)
        {
            await TestApi(new HttpMethod(method), ExploreEndpoint, ValidData, test: (response, content) =>
            {
                Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
                var jsonContent = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(content);
                Assert.True(jsonContent.ContainsKey("error"), $"Expected an 'error' object in payload: {content}");
            });
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

            await TestApi(HttpMethod.Post, ExploreEndpoint, data, test: (response, content) =>
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
            await TestApi(HttpMethod.Post, ExploreEndpoint, new { }, test: (response, content) =>
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
            var testConfig = factory.GetTestConfig(nameof(ApiTests), nameof(SuccessWithResult));
            var invalidData = new Models.ExploreParams
            {
                ApiKey = "INVALID_KEY",
                ApiUrl = ValidData.ApiUrl,
                DataSource = ValidData.DataSource,
                Table = ValidData.Table,
                Columns = ValidData.Columns,
            };

            var explorerGuid = await TestApi(
                HttpMethod.Post,
                ExploreEndpoint,
                data: invalidData,
                test: (response, content) =>
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

                    return explorerGuid;
                });

            await Task.Delay(2000);

            await TestExploreResult(HttpMethod.Get, explorerGuid, testConfig.PollFrequency, (response, content) =>
            {
                Assert.True(response.IsSuccessStatusCode, $"Response code {response.StatusCode}.");

                using var jsonContent = JsonDocument.Parse(content);
                var rootEl = jsonContent.RootElement;
                var status = rootEl.GetProperty("status").GetString();

                Assert.Equal("Error", status);

                const string expectedError = "Request Error: Unauthorized -- Your API token is wrong.";
                var errors = rootEl.GetProperty("errors").EnumerateArray().Select(e => e.GetString()).ToList();
                Assert.Contains(errors, e => e.Split("\n")[0] == expectedError);
            });
        }

        [Fact]
        public async Task FailWithInvalidExplorationId()
        {
            await TestApi(
                HttpMethod.Get,
                $"{ResultEndpoint}/11111111-1111-1111-1111-111111111111",
                null,
                test: (response, content) => Assert.True(response.StatusCode == HttpStatusCode.NotFound, content));
        }

        private async Task TestExploreResult(
            HttpMethod method,
            Guid explorerGuid,
            TimeSpan pollFrequency,
            ApiTestActionWithContent test,
            [CallerMemberName] string vcrSessionName = "",
            VcrSharp.VCRMode vcrMode = VcrSharp.VCRMode.Record)
        {
            const int pollMax = 10;

            for (var poll = 0; poll < pollMax; poll++)
            {
                using var response = await factory.SendExplorerApiRequest(
                    method,
                    $"{ResultEndpoint}/{explorerGuid}",
                    null,
                    nameof(ApiTests),
                    vcrSessionName,
                    vcrMode);

                var content = await response.Content.ReadAsStringAsync();
                using var jsonContent = JsonDocument.Parse(content);
                var rootEl = jsonContent.RootElement;
                Assert.True(
                    rootEl.ValueKind == JsonValueKind.Object,
                    $"Expected a JSON object in the response:\n{content}");

                Assert.True(
                    rootEl.TryGetProperty("status", out var statusEl),
                    $"Expected a 'status' property in:\n{content}");

                Assert.True(
                    rootEl.TryGetProperty("id", out var id),
                    $"Expected an 'id' property in:\n{content}");
                Assert.True(id.GetGuid() == explorerGuid);

                var status = statusEl.GetString();
                if (status == "Complete" || status == "Error" || status == "Canceled")
                {
                    test(response, content);
                    return;
                }

                Assert.True(poll < pollMax, $"Polled {pollMax} times without getting a result. Aborting.\n{content}");

                await Task.Delay(pollFrequency);
            }
        }

        private async Task TestApi(
            HttpMethod method,
            string endpoint,
            object? data,
            ApiTestActionWithContent test,
            [CallerMemberName] string vcrSessionName = "",
            VcrSharp.VCRMode vcrMode = VcrSharp.VCRMode.Record)
        {
            using var response = await factory.SendExplorerApiRequest(method, endpoint, data, nameof(ApiTests), vcrSessionName, vcrMode);
            var responseString = await response.Content.ReadAsStringAsync();
            test(response, responseString);
        }

        private async Task<T> TestApi<T>(
            HttpMethod method,
            string endpoint,
            object? data,
            ApiTestActionWithContent<T> test,
            [CallerMemberName] string vcrSessionName = "",
            VcrSharp.VCRMode vcrMode = VcrSharp.VCRMode.Record)
        {
            using var response = await factory.SendExplorerApiRequest(method, endpoint, data, nameof(ApiTests), vcrSessionName, vcrMode);
            var responseString = await response.Content.ReadAsStringAsync();
            return test(response, responseString);
        }
    }
}
