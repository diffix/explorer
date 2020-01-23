namespace Explorer.Api.Tests
{
    using System;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Text.Json;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.TestHost;
    using Xunit;

    public sealed class ExploreTest
    {
        private delegate void ApiTestActionWithContent(HttpResponseMessage response, string content);

        [Fact]
        public void Success()
        {
            var data = new Models.ExploreParams { ApiKey = "apikey", DataSourceName = "ds", TableName = "tn", ColumnName = "cn" };
            TestApi(HttpMethod.Post, "/explore", data, (response, content) =>
                Assert.True(response.IsSuccessStatusCode, content));
        }

        [Theory]
        [InlineData("")]
        [InlineData("/")]
        [InlineData("/invalid endpoint test")]
        public void FailWithBadEndPoint(string endpoint)
        {
            var data = new Models.ExploreParams { ApiKey = "apikey", DataSourceName = "ds", TableName = "tn", ColumnName = "cn" };
            TestApi(HttpMethod.Post, endpoint, data, (response, content) =>
                Assert.True(response.StatusCode == HttpStatusCode.NotFound, content));
        }

        [Theory]
        [InlineData("DELETE")]
        [InlineData("GET")]
        [InlineData("HEAD")]
        [InlineData("PUT")]
        public void FailWithBadMethod(string method)
        {
            var data = new Models.ExploreParams { ApiKey = "apikey", DataSourceName = "ds", TableName = "tn", ColumnName = "cn" };
            TestApi(new HttpMethod(method), "/explore", data, (response, content) =>
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

        private async void TestApi(HttpMethod method, string endpoint, object data, ApiTestActionWithContent test)
        {
            using var server = new TestServer(new WebHostBuilder()
                .UseEnvironment("Development")
                .UseStartup<Startup>());
            using var client = server.CreateClient();
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
