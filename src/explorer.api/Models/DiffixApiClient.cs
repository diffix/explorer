using System;
using Explorer.Api.Models;
using System.Net.Http;
using System.Net;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using System.Net.Http.Headers;

namespace Explorer.Api.DiffixApi
{
    interface IDiffixApi
    {
        Task<DataSources> GetDataSources();
        Task<QueryId> Query(string statement);
        Task<T> QueryResult<T>(string queryId);
        Task<CancelSuccess> CancelQuery(string queryId);
    }

    static class DiffixApi
    {
        static private readonly DiffixApiClient ApiClient = new DiffixApiClient();

        static DiffixApiSession NewSession(string apiRootUrl, string apiKey) {
            return new DiffixApiSession(ApiClient, apiRootUrl, apiKey);
        }
    }


    class DiffixApiSession : IDiffixApi
    {
        private static DiffixApiClient ApiClient;
        private readonly string ApiRootUrl;
        private readonly string ApiKey;

        public DiffixApiSession(DiffixApiClient apiClient, string apiRootUrl, string apiKey)
        {
            ApiClient = apiClient;
            ApiKey = apiKey;
            ApiRootUrl = apiRootUrl;
        }

        public Task<DataSources> GetDataSources()
        {
            throw new NotImplementedException();
        }
        public Task<QueryId> Query(string statement)
        {
            throw new NotImplementedException();
        }
        public Task<T> QueryResult<T>(string queryId)
        {
            throw new NotImplementedException();
        }
        public Task<CancelSuccess> CancelQuery(string queryId)
        {
            throw new NotImplementedException();
        }
    }

    class DiffixApiClient : HttpClient
    {
        async private Task<JsonDocument> ApiGetRequest(
            string apiEndpoint,
            string apiKey)
        {
            var requestMessage =
                new HttpRequestMessage(HttpMethod.Get, apiEndpoint);

            requestMessage.Headers.Authorization =
                new AuthenticationHeaderValue(apiKey);

            var response = await SendAsync(requestMessage);

            if (response.StatusCode == HttpStatusCode.Accepted)
            {
                var contentStream = await response.Content.ReadAsStreamAsync();
                return await JsonDocument.ParseAsync(contentStream);
            }
            else
            {
                throw new Exception($"GET Request Error: {serviceError(response)}");
            }
        }

        async private Task<JsonDocument> ApiPostRequest(
            string apiEndpoint,
            string apiToken,
            string requestContent = default)
        {
            var requestMessage =
                new HttpRequestMessage(HttpMethod.Post, apiEndpoint);

            requestMessage.Headers.Authorization = new AuthenticationHeaderValue(apiToken);

            requestMessage.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            requestMessage.Content = new StringContent(requestContent.ToString());

            var response = await SendAsync(requestMessage);
            var contentStream = await response.Content.ReadAsStreamAsync();

            return await JsonDocument.ParseAsync(contentStream);
        }

        private string serviceError(HttpResponseMessage response)
        {
            return response.StatusCode switch
            {
                HttpStatusCode.Unauthorized =>
                    "Unauthorized -- Your API token is wrong",
                HttpStatusCode.NotFound =>
                    "Not Found -- Invalid URL",
                HttpStatusCode.InternalServerError =>
                    "Internal Server Error -- We had a problem with our server. Try again later.",
                HttpStatusCode.ServiceUnavailable =>
                    "Service Unavailable -- We're temporarily offline for maintenance. Please try again later.",
                HttpStatusCode.GatewayTimeout =>
                    "Gateway Timeout -- A timeout occured while contacting the data source. " +
                    "The system might be overloaded. Try again later.",
                _ => throw new NotImplementedException(),
            };
        }
    }
}