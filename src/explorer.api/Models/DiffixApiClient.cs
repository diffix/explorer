using System;
using System.Net.Http;
using System.Net;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using System.Linq;

namespace Explorer.Api.DiffixApi
{
    using DataSources = List<DataSource>;

    public interface IDiffixApi
    {
        Task<DataSources> GetDataSources();
        Task<QueryResponse> Query(string dataSource, string queryStatement);
        Task<QueryResult<T>> PollQueryResult<T>(string queryId);
        Task<CancelSuccess> CancelQuery(string queryId);
    }

    public static class DiffixApi
    {
        static private readonly DiffixApiClient ApiClient = new DiffixApiClient();

        public static DiffixApiSession NewSession(string apiRootUrl, string apiKey)
        {
            return new DiffixApiSession(ApiClient, apiRootUrl, apiKey);
        }
    }

    public class SnakeCaseNamingPolicy : JsonNamingPolicy
    {
        public override string ConvertName(string pascalCase)
        {
            var fragments = Regex.Matches(pascalCase, "[A-Z]+[a-z]+")
                .Select(match => match.Value.ToLower());
            return String.Join("_", fragments);
        }
    }

    public class DataSource
    {
        public struct Table
        {
            public struct Column
            {
                public enum DiffixType
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
                DiffixType Type { get; set; }
            }
            string Id { get; set; }
            IEnumerable<Column> Columns { get; set; }
        }

        public string Name { get; set; }
        public string Description { get; set; }
        IEnumerable<Table> Tables { get; set; }
    }

    public class QueryResult<RowType>
    {
        public struct RowWithCount
        {
            public List<RowType> Row { get; set; }
            public int Occurrences { get; set; }
        }
        public bool Completed { get; set; }

        // "query_state": "<the execution phase of the query>",
        public string QueryState { get; set; }

        // "id": "<query-id>",
        public string Id { get; set; }

        // "statement": "<query-statement>",
        public string Statement { get; set; }

        // "error": "<error-message>",
        public string Error { get; set; }

        // "columns": ["<column-name>", ...],
        public List<string> Columns { get; set; }

        // "row_count": <row-count>,
        public int RowCount { get; set; }

        // "rows": [
        // {"row": [<value-1>, ...], "occurrences": <number-of-occurrences>},
        // ...
        // ]
        public List<RowWithCount> Rows { get; set; }
    }

    public struct QueryResponse
    {
        bool Success { get; set; }
        string QueryId { get; set; }
    }

    public struct CancelSuccess
    {
        bool Success { get; set; }
    }

    public class DiffixApiSession : IDiffixApi
    {
        private DiffixApiClient ApiClient;
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
                new Uri(ApiRootUrl, "data_sources"),
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
                    statement = queryStatement,
                    data_source_name = dataSource
                }
            };

            return await ApiClient.ApiPostRequest<QueryResponse>(
                EndPointUrl("query"),
                ApiKey,
                JsonSerializer.Serialize(queryBody)
                );
        }

        async public Task<QueryResult<RowType>> PollQueryResult<RowType>(string queryId)
        {
            return await ApiClient.ApiGetRequest<QueryResult<RowType>>(
                EndPointUrl($"query/{queryId}"),
                ApiKey
                );
        }

        async public Task<QueryResult<RowType>> PollQueryUntilComplete<RowType>(
            string queryId,
            CancellationToken ct,
            TimeSpan? pollFrequency)
        {
            if (pollFrequency is null)
            {
                pollFrequency = TimeSpan.FromMilliseconds(500);
            }

            while (!ct.IsCancellationRequested)
            {
                var queryResult = await PollQueryResult<RowType>(queryId);
                if (queryResult.Completed)
                {
                    return queryResult;
                }
                else
                {
                    await Task.Delay(pollFrequency.Value, ct);
                }
            };

            ct.ThrowIfCancellationRequested();

            throw new Exception("Should never reach here.");
        }

        async public Task<QueryResult<RowType>> PollQueryUntilCompleteOrTimeout<RowType>(
            string queryId,
            TimeSpan? pollFrequency,
            TimeSpan? timeout = null)
        {
            if (timeout is null)
            {
                timeout = TimeSpan.FromMinutes(10);
            }

            using var tokenSource = new CancellationTokenSource();

            try
            {
                return await PollQueryUntilComplete<RowType>(queryId, tokenSource.Token, pollFrequency);
            }
            catch (OperationCanceledException e)
            {
                if (e.CancellationToken == tokenSource.Token)
                {
                    throw new TimeoutException($"Timed out while waiting for query results for {queryId}");
                }

                throw;
            }
        }

        async public Task<CancelSuccess> CancelQuery(string queryId)
        {
            return await ApiClient.ApiPostRequest<CancelSuccess>(
                EndPointUrl($"{queryId}/cancel"),
                ApiKey
            );
        }

        private Uri EndPointUrl(string path)
        {
            return new Uri(ApiRootUrl, path);
        }
    }

    public class DiffixApiClient : HttpClient
    {
        /// <summary>
        /// Send a GET request to the Diffix API. Handles authentication.
        /// </summary>
        /// <param name="apiEndpoint">The API endpoint to target.</param>
        /// <param name="apiKey">The API key for the service.</param>
        /// <typeparam name="T"></typeparam>
        /// <returns>A <code>Task&lt;T&gt;<code> which, upon completion, contains the API response deserialized
        /// to the provided return type.</returns>
        /// <exception cref="HttpRequestException"></exception>
        /// <exception cref="JsonException">The JSON is invalid. 
        /// -or- <code>T<code> is not compatible with the JSON. 
        /// -or- There is remaining data in the stream.</exception>
        async public Task<T> ApiGetRequest<T>(
            Uri apiEndpoint,
            string apiKey)
        {
            return await ApiRequest<T>(HttpMethod.Get, apiEndpoint, apiKey);
        }

        /// <summary>
        /// Send a POST request to the Diffix API. Handles authentication.
        /// </summary>
        /// <param name="apiEndpoint">The API endpoint to target</param>
        /// <param name="apiKey">The API key for the service</param>
        /// <param name="requestContent">JSON-encoded request message (optional)</param>
        /// <typeparam name="T"></typeparam>
        /// <returns>A <code>Task&lt;T&gt;<code> which, upon completion, contains the API response deserialized
        /// to the provided return type</returns>
        /// <exception cref="HttpRequestException"></exception>
        /// <exception cref="JsonException">The JSON is invalid. 
        /// -or- <code>T<code> is not compatible with the JSON. 
        /// -or- There is remaining data in the stream.</exception>
        async public Task<T> ApiPostRequest<T>(
            Uri apiEndpoint,
            string apiKey,
            string requestContent = default)
        {
            return await ApiRequest<T>(HttpMethod.Post, apiEndpoint, apiKey, requestContent);
        }

        /// <summary>
        /// Send a request to the Diffix API. Handles authentication 
        /// </summary>
        /// <param name="requestMethod">The HTTP method to use in the request</param>
        /// <param name="apiEndpoint">The API endpoint to target</param>
        /// <param name="apiKey">The API key for the service</param>
        /// <param name="requestContent">JSON-encoded request message (optional)</param>
        /// <typeparam name="T"></typeparam>
        /// <returns>A <code>Task&lt;T&gt;<code> which, upon completion, contains the API response deserialized
        /// to the provided return type</returns>
        /// <exception cref="HttpRequestException"></exception>
        /// <exception cref="JsonException">The JSON is invalid. 
        /// -or- <code>T<code> is not compatible with the JSON. 
        /// -or- There is remaining data in the stream.</exception>
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

            if (!(requestContent is null))
            {
                requestMessage.Content = new StringContent(requestContent);
                requestMessage.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            }

            using var response = await SendAsync(requestMessage);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                using var contentStream = await response.Content.ReadAsStreamAsync();
                var opts = new JsonSerializerOptions
                {
                    Converters = { new JsonStringEnumConverter(new SnakeCaseNamingPolicy()) },
                    PropertyNamingPolicy = new SnakeCaseNamingPolicy()
                };
                return await JsonSerializer.DeserializeAsync<T>(contentStream, opts);
            }
            else
            {
                throw new HttpRequestException($"{requestMethod} Request Error: {serviceError(response)}");
            }
        }

        /// <summary>
        /// Turns the HTTP response into a custom error string
        /// </summary>
        /// <param name="response">The HTTP response code</param>
        /// <returns>A string containing a custom error message</returns>
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
                _ => response.StatusCode.ToString(),
            };
        }
    }
}