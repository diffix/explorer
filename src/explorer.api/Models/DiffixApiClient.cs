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

    /// <summary>
    /// Helper class for converting Json keys from the .NET standard PascalCase to snake_case
    /// </summary>
    public class SnakeCaseNamingPolicy : JsonNamingPolicy
    {
        public override string ConvertName(string pascalCase)
        {
            var fragments = Regex.Matches(pascalCase, "[A-Z]+[a-z]+")
                .Select(match => match.Value.ToLower());
            return String.Join("_", fragments);
        }
    }



    /// <summary>
    /// Helper type representing the JSON response from a request to /api/data_sources.
    /// </summary>
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

    /** <summary>
        Helper type representing the JSON response from a request to /api/queries/{query_id}.

        Example response: 
        <code>
        {
            &quot;query&quot;: {
                &quot;buckets_link&quot;: &quot;/queries/9c08137a-b69f-450c-a13c-383340ddda2c/buckets&quot;,
                &quot;completed&quot;: true,
                &quot;data_source&quot;: {
                    &quot;name&quot;: &quot;gda_banking&quot;
                },
                &quot;data_source_id&quot;: 9,
                &quot;id&quot;: &quot;9c08137a-b69f-450c-a13c-383340ddda2c&quot;,
                &quot;inserted_at&quot;: &quot;2020-01-15T13:42:09.255580&quot;,
                &quot;private_permalink&quot;: &quot;/permalink/private/query/[...],
                &quot;public_permalink&quot;: &quot;/permalink/public/query/[...],
                &quot;query_state&quot;: &quot;completed&quot;,
                &quot;session_id&quot;: null,
                &quot;statement&quot;: &quot;select count(*), count_noise(*) from loans&quot;,
                &quot;user&quot;: {
                    &quot;name&quot;: &quot;Daniel Lennon&quot;
                },
                &quot;columns&quot;: [
                    &quot;count&quot;,
                    &quot;count_noise&quot;
                ],
                &quot;error&quot;: null,
                &quot;info&quot;: [
                    &quot;[Debug] Using statistics-based anonymization.&quot;,
                    &quot;[Debug] Query executed in 0.255 seconds.&quot;
                ],
                &quot;log&quot;: &quot;2020-01-15 [...] [info] query finished\n&quot;,
                &quot;row_count&quot;: 1,
                &quot;types&quot;: [
                    &quot;integer&quot;,
                    &quot;real&quot;
                ],
                &quot;rows&quot;: [
                {
                    &quot;unreliable&quot;: false,
                    &quot;row&quot;: [
                    825,
                    1
                    ],
                    &quot;occurrences&quot;: 1
                }
                ]
            }
        }
        </code>
        </summary>
        <typeparam name="RowType"></typeparam>
    */
    public struct QueryResult<RowType>
    {
        public struct QueryRowsWithCount
        {
            public RowType Row { get; set; }
            public bool Unreliable { get; set; }
            public int Occurrences { get; set; }
        }
        public struct QueryUser
        {
            public string Name { get; set; }
        }

        public struct QueryDataSource
        {
            public string Name { get; set; }
        }
        public string BucketsLink { get; set; }
        public bool Completed { get; set; }
        public QueryDataSource DataSource { get; set; }
        public string DataSourceId { get; set; }
        public string Id { get; set; }
        public System.DateTime InsertedAt { get; set; }

        public string PrivatePermalink { get; set; }
        public string PublicPermalink { get; set; }
        public string QueryState { get; set; }
        public string SessionId { get; set; }
        public string Statement { get; set; }
        public QueryUser User { get; set; }
        public List<string> Columns { get; set; }
        public string Error { get; set; }
        public List<string> Info { get; set; }
        public string Log { get; set; }
        public int RowCount { get; set; }
        public List<string> Types { get; set; }
        public List<QueryRowsWithCount> Rows { get; set; }
    }

    /// <summary>
    /// Helper type representing the JSON response from a POST request to /api/query.
    /// </summary>
    public struct QueryResponse
    {
        public bool Success { get; set; }
        public string QueryId { get; set; }
    }

    /// <summary>
    /// Helper type representing the JSON response from a request to /api/queries/{query_id}/cancel.
    /// </summary>
    public struct CancelSuccess
    {
        public bool Success { get; set; }
    }

    public class DiffixApiSession : IDiffixApi
    {
        private DiffixApiClient ApiClient;
        private readonly Uri ApiRootUrl;
        private readonly string ApiKey;
        private const int DEFAULT_POLLING_FREQUENCY_MS = 2000;

        public DiffixApiSession(
            DiffixApiClient apiClient,
            string apiRootUrl,
            string apiKey)
        {
            ApiClient = apiClient;
            ApiKey = apiKey;
            ApiRootUrl = new Uri(apiRootUrl);
        }

        /// <summary>
        /// Sends a Http GET request to the Aircloak server's /api/data_sources endpoint. 
        /// </summary>
        /// <returns>A DataSources instance containing the return list of data sources provided by this 
        /// Aircloak instance.</returns>
        async public Task<DataSources> GetDataSources()
        {
            return await ApiClient.ApiGetRequest<DataSources>(
                new Uri(ApiRootUrl, "data_sources"),
                ApiKey);
        }

        /// <summary>
        /// Sends a Http POST request to the Aircloak server's /api/queries endpoint.
        /// </summary>
        /// <param name="dataSource">The data source to run the query against.</param>
        /// <param name="queryStatement">The query statement as a string</param>
        /// <returns>A QueryResonse instance containing the success status and query Id</returns>
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
                EndPointUrl("queries"),
                ApiKey,
                JsonSerializer.Serialize(queryBody)
                );
        }

        /// <summary>
        /// Sends a Http POST request to the Aircloak server's /api/queries/{query_id} endpoint. 
        /// </summary>
        /// <param name="queryId">The query Id obtained via a previous call to the /api/query endpoint.</param>
        /// <typeparam name="RowType">The type to use to deserialise each row returned in the query results.</typeparam>
        /// <returns>A QueryResult instance. If the query has finished executing, contains the query results, with each 
        /// row seralised to type <code>RowType</code></returns>
        async public Task<QueryResult<RowType>> PollQueryResult<RowType>(string queryId)
        {
            return await ApiClient.ApiGetRequest<QueryResult<RowType>>(
                EndPointUrl($"queries/{queryId}"),
                ApiKey
                );
        }

        /// <summary>
        /// Calls <code>PollQueryResult</code> in a loop until the query completes. Can be canceled.
        /// </summary>
        /// <remarks>
        /// Canceling via the <code>CancellationToken</code> cancels the returned <code>Task</code> but does not
        /// cancel query execution. To do this, a call to <code>/api/queries/{query_id}/cancel</code> must be made.  
        /// </remarks>
        /// <param name="queryId">The query Id obtained via a previous call to the /api/query endpoint.</param>
        /// <param name="ct">A <code>CancellationToken</code> that cancels the returned <code>Task</code>.</param>
        /// <param name="pollFrequency">How often to poll the api endpoint. Default is DEFAULT_POLLING_FREQUENCY_MS
        /// </param>
        /// <typeparam name="RowType">The type to use to deserialise each row returned in the query results.</typeparam>
        /// <returns>A QueryResult instance. If the query has finished executing, contains the query results, with each 
        /// row seralised to type <code>RowType</code></returns>
        async public Task<QueryResult<RowType>> PollQueryUntilComplete<RowType>(
            string queryId,
            CancellationToken ct,
            TimeSpan? pollFrequency = null)
        {
            if (pollFrequency is null)
            {
                pollFrequency = TimeSpan.FromMilliseconds(DEFAULT_POLLING_FREQUENCY_MS);
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

        /// <summary>
        /// Polls for a query's results until query resolution is complete, or until a specified timeout.
        /// </summary>
        /// <param name="queryId">The query Id obtained via a previous call to the /api/query endpoint.</param>
        /// <param name="timeout">How long to wait for the query to complete.</param>
        /// <param name="pollFrequency">Optional. How often to poll the api endpoint. Defaults to 
        /// DEFAULT_POLLING_FREQUENCY_MS.</param>
        /// <typeparam name="RowType">The type to use to deserialise each row returned in the query results.</typeparam>
        /// <returns>A QueryResult instance. If the query has finished executing, contains the query results, with each 
        /// row seralised to type <code>RowType</code></returns>
        async public Task<QueryResult<RowType>> PollQueryUntilCompleteOrTimeout<RowType>(
            string queryId,
            TimeSpan timeout,
            TimeSpan? pollFrequency = null)
        {
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

        /// <summary>
        /// Sends a Http GET request to the /api/queries/{query_id}/cancel. Cancels a running query on the Aircloak 
        /// server. 
        /// </summary>
        /// <param name="queryId">The id of the query to cancel</param>
        /// <returns>A <code>CancelSuccess</code> instance indicating whether or not the query was indeed canceled.
        /// </returns>
        async public Task<CancelSuccess> CancelQuery(string queryId)
        {
            return await ApiClient.ApiPostRequest<CancelSuccess>(
                EndPointUrl($"queries/{queryId}/cancel"),
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

            if (response.IsSuccessStatusCode)
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