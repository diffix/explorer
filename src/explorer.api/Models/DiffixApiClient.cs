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

        static DiffixApiSession NewSession(string apiRootUrl, string apiKey)
        {
            return new DiffixApiSession(ApiClient, apiRootUrl, apiKey);
        }
    }

    struct DataSources
    {
        struct Table
        {
            struct Column
            {
                enum DiffixType
                {
                    Integer,
                    Real,
                    Text,
                    Timestamp,
                    Date,
                    Datetime,
                    Bool,

                }
                string Name { get; set; }
                DiffixType DType { get; set; }
            }
            string Id { get; set; }
            IEnumerable<Column> columns { get; set; }
        }
        string Name { get; set; }
        string Description { get; set; }

        IEnumerable<Table> tables { get; set; }
    }

    struct QueryId
    {
        string Id { get; set; }
    }

    struct CancelSuccess
    {
        bool IsSuccess { get; set; }
    }

    class DiffixApiSession : IDiffixApi
    {
        private static DiffixApiClient ApiClient;
        private readonly Uri ApiRootUrl;
        private readonly string ApiKey;

        public DiffixApiSession(DiffixApiClient apiClient, string apiRootUrl, string apiKey)
        {
            ApiClient = apiClient;
            ApiKey = apiKey;
            ApiRootUrl = new Uri(apiRootUrl);
        }

        async public Task<DataSources> GetDataSources()
        {
            var responseJson = await ApiClient.ApiGetRequest(
                new Uri(ApiRootUrl, "data_source"),
                ApiKey);

            // Parse JSON
            return await Task.FromException<DataSources>(new NotImplementedException());
        }
        async public Task<QueryId> Query(string statement)
        {
            var responseJson = await ApiClient.ApiPostRequest(
                new Uri(ApiRootUrl, "query"),
                ApiKey,
                );


        }
        async public Task<T> QueryResult<T>(string queryId)
        {
            return await Task.FromException<T>(new NotImplementedException());
        }
        async public Task<CancelSuccess> CancelQuery(string queryId)
        {
            return await Task.FromException(new NotImplementedException());
        }
    }

    class DiffixApiClient : HttpClient
    {
        async public Task<JsonDocument> ApiGetRequest(
            Uri apiEndpoint,
            string apiKey)
        {
            using var requestMessage =
                new HttpRequestMessage(HttpMethod.Get, apiEndpoint);

            requestMessage.Headers.Authorization =
                new AuthenticationHeaderValue(apiKey);

            using var response = await SendAsync(requestMessage);

            if (response.StatusCode == HttpStatusCode.Accepted)
            {
                using var contentStream = await response.Content.ReadAsStreamAsync();
                return await JsonDocument.ParseAsync(contentStream);
            }
            else
            {
                throw new Exception($"GET Request Error: {serviceError(response)}");
            }
        }

        async public Task<JsonDocument> ApiPostRequest(
            Uri apiEndpoint,
            string apiToken,
            string requestContent = default)
        {
            using var requestMessage =
                new HttpRequestMessage(HttpMethod.Post, apiEndpoint);

            requestMessage.Headers.Authorization = new AuthenticationHeaderValue(apiToken);

            requestMessage.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            requestMessage.Content = new StringContent(requestContent);

            using var response = await SendAsync(requestMessage);
            using var contentStream = await response.Content.ReadAsStreamAsync();

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