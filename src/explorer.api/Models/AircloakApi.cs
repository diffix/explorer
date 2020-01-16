using System;
using System.Net.Http;
using System.Net;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Http.Headers;

using Explorer.Api.AircloakApi.ReponseTypes;
using Explorer.Api.AircloakApi.Helpers;

namespace Explorer.Api.AircloakApi
{
    using DataSources = List<DataSource>;

    /// <summary>
    /// Contains the HttpClient instance and doles out ApiSession objects.
    /// </summary>
    public static class SessionManager
    {
        static private readonly AircloakApiClient ApiClient = new AircloakApiClient();

        public static AircloakApiSession NewSession(string apiRootUrl, string apiKey)
        {
            return new AircloakApiSession(ApiClient, apiRootUrl, apiKey);
        }
    }

    /// <summary>
    /// Provides higher-level access to the Aircloak Http Api. The session uses the same Url root and Api Key 
    /// throughout its lifetime. 
    /// </summary>
    /// <see cref=AircloakApiClient/>
    public class AircloakApiSession
    {
        private readonly AircloakApiClient ApiClient;
        private readonly Uri ApiRootUrl;
        private readonly string ApiKey;
        private const int DEFAULT_POLLING_FREQUENCY_MS = 2000;

        /// <summary>
        /// Create a new <c>AircloakApiSession</c> instance.
        /// </summary>
        /// <param name="apiClient">An <c>AircloakApiClient</c> instance. The <paramref name="apiClient"/> is based on 
        /// the .NET <c>HttpClient</c> class and should be reused, ie. the <paramref name="apiClient"/> should be a
        /// reference to singleton instance.</param>
        /// <param name="apiRootUrl">The root Url for the Aircloak Api, eg. "https://attack.aircloak.com/api/".</param>
        /// <param name="apiKey">The Api key to use for this session.</param>
        /// <see cref=AircloakApiClient/>
        public AircloakApiSession(
            AircloakApiClient apiClient,
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
        /// <returns>A <c>List&lt;DataSource&gt;</c> containing the data sources provided by this 
        /// Aircloak instance.</returns>
        async public Task<DataSources> GetDataSources()
        {
            return await ApiClient.ApiGetRequest<DataSources>(
                new Uri(ApiRootUrl, "data_sources"),
                ApiKey);
        }

        async public Task<QueryResult<RowType>> Query<RowType>(
            string dataSource,
            string queryStatement,
            TimeSpan timeout)
        {
            var queryResponse = await SubmitQuery(dataSource, queryStatement);
            if (!queryResponse.Success)
            {
                throw new Exception($"Unhandled Aircloak error returned for query to {dataSource}. Query Statement:" +
                                    $" {queryStatement}.");
            }

            return await PollQueryUntilCompleteOrTimeout<RowType>(queryResponse.QueryId, timeout);
        }
        /// <summary>
        /// Sends a Http POST request to the Aircloak server's /api/queries endpoint.
        /// </summary>
        /// <param name="dataSource">The data source to run the query against.</param>
        /// <param name="queryStatement">The query statement as a string</param>
        /// <returns>A QueryResonse instance containing the success status and query Id</returns>
        async public Task<QueryResponse> SubmitQuery(
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
        /// row seralised to type <c>RowType</c></returns>
        async public Task<QueryResult<RowType>> PollQueryResult<RowType>(string queryId)
        {
            return await ApiClient.ApiGetRequest<QueryResult<RowType>>(
                EndPointUrl($"queries/{queryId}"),
                ApiKey
                );
        }

        /// <summary>
        /// Calls <c>PollQueryResult</c> in a loop until the query completes. Can be canceled.
        /// </summary>
        /// <remarks>
        /// Canceling via the <c>CancellationToken</c> cancels the returned <c>Task</c> but does not
        /// cancel query execution. To do this, a call to <c>/api/queries/{query_id}/cancel</c> must be made.  
        /// </remarks>
        /// <param name="queryId">The query Id obtained via a previous call to the /api/query endpoint.</param>
        /// <param name="ct">A <c>CancellationToken</c> that cancels the returned <c>Task</c>.</param>
        /// <param name="pollFrequency">How often to poll the api endpoint. Default is DEFAULT_POLLING_FREQUENCY_MS
        /// </param>
        /// <typeparam name="RowType">The type to use to deserialise each row returned in the query results.</typeparam>
        /// <returns>A QueryResult instance. If the query has finished executing, contains the query results, with each 
        /// row seralised to type <c>RowType</c></returns>
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
                // Note: the cancellation token is not currently passed through to the PollQueryResult call. The 
                // assumption here is that polling should return a result immediately. In practice, this may not be the
                // case due to network delay. In this case, cancellation requests may take longer to take effect than 
                // expected. In future we could thread the cancellation token right down through to the raw SyncSend
                // call in the HttpClient.
                var queryResult = await PollQueryResult<RowType>(queryId);
                if (queryResult.Query.Completed)
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
        /// row seralised to type <c>RowType</c></returns>
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
        /// <returns>A <c>CancelSuccess</c> instance indicating whether or not the query was indeed canceled.
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

    /// <summary>
    /// Convenience class derived from <c>HttpClient</c> provides GET and POST methods adapted to the 
    /// Aircloak API: 
    /// <list type=bullet>
    /// <item>
    /// <description>Sets the provided Api Key on all outgoing requests.</description>
    /// </item>
    /// <item>
    /// <description>Augments unsuccessful requests with custom error messages. </description>
    /// </item>
    /// <item>
    /// <description>Deserializes Json responses.</description>
    /// </item>
    /// </list>
    /// </summary>
    public class AircloakApiClient : HttpClient
    {
        /// <summary>
        /// Send a GET request to the Aircloak API. Handles authentication.
        /// </summary>
        /// <param name="apiEndpoint">The API endpoint to target.</param>
        /// <param name="apiKey">The API key for the service.</param>
        /// <typeparam name="T"></typeparam>
        /// <returns>A <c>Task&lt;T&gt;</c> which, upon completion, contains the API response deserialized
        /// to the provided return type.</returns>
        /// <exception cref="HttpRequestException"></exception>
        /// <exception cref="JsonException">The JSON is invalid. 
        /// -or- <c>T</c> is not compatible with the JSON. 
        /// -or- There is remaining data in the stream.</exception>
        async public Task<T> ApiGetRequest<T>(
            Uri apiEndpoint,
            string apiKey)
        {
            return await ApiRequest<T>(HttpMethod.Get, apiEndpoint, apiKey);
        }

        /// <summary>
        /// Send a POST request to the Aircloak API. Handles authentication.
        /// </summary>
        /// <param name="apiEndpoint">The API endpoint to target</param>
        /// <param name="apiKey">The API key for the service</param>
        /// <param name="requestContent">JSON-encoded request message (optional)</param>
        /// <typeparam name="T"></typeparam>
        /// <returns>A <c>Task&lt;T&gt;</c> which, upon completion, contains the API response deserialized
        /// to the provided return type</returns>
        /// <exception cref="HttpRequestException"></exception>
        /// <exception cref="JsonException">The JSON is invalid. 
        /// -or- <c>T</c> is not compatible with the JSON. 
        /// -or- There is remaining data in the stream.</exception>
        async public Task<T> ApiPostRequest<T>(
            Uri apiEndpoint,
            string apiKey,
            string requestContent = default)
        {
            return await ApiRequest<T>(HttpMethod.Post, apiEndpoint, apiKey, requestContent);
        }

        /// <summary>
        /// Send a request to the Aircloak API. Handles authentication 
        /// </summary>
        /// <param name="requestMethod">The HTTP method to use in the request</param>
        /// <param name="apiEndpoint">The API endpoint to target</param>
        /// <param name="apiKey">The API key for the service</param>
        /// <param name="requestContent">JSON-encoded request message (optional)</param>
        /// <typeparam name="T"></typeparam>
        /// <returns>A <c>Task&lt;T&gt;</c> which, upon completion, contains the API response deserialized
        /// to the provided return type</returns>
        /// <exception cref="HttpRequestException"></exception>
        /// <exception cref="JsonException">The JSON is invalid. 
        /// -or- <c>T</c> is not compatible with the JSON. 
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

            using var response = await SendAsync(
                requestMessage,
                HttpCompletionOption.ResponseHeadersRead);

            if (response.IsSuccessStatusCode)
            {
                using var contentStream = await response.Content.ReadAsStreamAsync();
                var opts = new JsonSerializerOptions
                {
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
        /// <param name="response">The HTTP response c</param>
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