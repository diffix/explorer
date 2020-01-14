using System;
using System.Net.Http;
using System.Net;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Net.Http.Headers;

namespace Explorer.Api.DiffixApi
{
    interface IDiffixApi
    {
        Task<DataSources> GetDataSources();
        Task<QueryResponse> Query(string dataSource, string queryStatement);
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

    struct QueryResponse
    {
        [JsonPropertyName("success")]
        bool Success { get; set; }
        [JsonPropertyName("query_id")]
        string QueryId { get; set; }
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

        public DiffixApiSession(
            DiffixApiClient apiClient,
            string apiRootUrl,
            string apiKey)
        {
            ApiClient = apiClient;
            ApiKey = apiKey;
            ApiRootUrl = new Uri(apiRootUrl);
        }

        async public Task<DataSources> GetDataSources()
        {
            return await ApiClient.ApiGetRequest<DataSources>(
                new Uri(ApiRootUrl, "data_source"),
                ApiKey);
        }
        async public Task<QueryResponse> Query(
            string dataSource,
            string queryStatement)
        {
            var queryBody = new
            {
                query = new
                {
                    query = queryStatement,
                    data_source_name = dataSource
                }
            };

            return await ApiClient.ApiPostRequest<QueryResponse>(
                new Uri(ApiRootUrl, "query"),
                ApiKey,
                JsonSerializer.Serialize(queryBody)
                );
        }
        async public Task<T> QueryResult<T>(string queryId)
        {
            return await Task.FromException<T>(new NotImplementedException());
        }
        async public Task<CancelSuccess> CancelQuery(string queryId)
        {
            return await Task.FromException<CancelSuccess>(new NotImplementedException());
        }
    }

    class DiffixApiClient : HttpClient
    {
        async public Task<T> ApiGetRequest<T>(
            Uri apiEndpoint,
            string apiKey)
        {
            return await ApiRequest<T>(HttpMethod.Get, apiEndpoint, apiKey);
        }

        async public Task<T> ApiPostRequest<T>(
            Uri apiEndpoint,
            string apiKey,
            string requestContent = default)
        {
            return await ApiRequest<T>(HttpMethod.Post, apiEndpoint, apiKey, requestContent);
        }

        async private Task<T> ApiRequest<T>(
            HttpMethod requestMethod,
            Uri apiEndpoint,
            string apiKey,
            string requestContent = default)
        {

            using var requestMessage =
                new HttpRequestMessage(requestMethod, apiEndpoint);

            if (!requestMessage.Headers.TryAddWithoutValidation("auth-token", apiKey))
            {
                throw new Exception($"Failed to add Http header 'auth-token: {apiKey}'");
            }

            requestMessage.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            requestMessage.Content = new StringContent(requestContent);

            using var response = await SendAsync(requestMessage);

            if (response.StatusCode == HttpStatusCode.Accepted)
            {
                using var contentStream = await response.Content.ReadAsStreamAsync();
                return await JsonSerializer.DeserializeAsync<T>(contentStream);
            }
            else
            {
                throw new Exception($"{requestMethod} Request Error: {serviceError(response)}");
            }
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