namespace Aircloak.JsonApi
{
    using System;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;

    using Aircloak.JsonApi.ResponseTypes;

    /// <summary>
    /// Convenience class providing GET and POST methods adapted to the
    /// Aircloak API:
    /// <list type="bullet">
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
    public class JsonApiClient
    {
        private const int DefaultPollingFrequencyMillis = 2000;

        private static readonly JsonSerializerOptions DefaultJsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = new SnakeCaseNamingPolicy(),
        };

        private readonly HttpClient httpClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonApiClient" /> class.
        /// </summary>
        /// <param name="httpClient">A HttpClient object injected into this instance.</param>
        public JsonApiClient(HttpClient httpClient)
        {
            this.httpClient = httpClient;
        }

        /// <summary>
        /// Sends a Http GET request to the Aircloak server's /api/data_sources endpoint.
        /// </summary>
        /// <returns>A <c>List&lt;DataSource&gt;</c> containing the data sources provided by this
        /// Aircloak instance.</returns>
        public async Task<DataSourceCollection> GetDataSources()
        {
            return await ApiGetRequest<DataSourceCollection>("data_sources");
        }

        /// <summary>
        /// Posts a query to the Aircloak server, retrieves the query ID, and then polls for the result.
        /// </summary>
        /// <param name="dataSource">The data source to run the query against.</param>
        /// <param name="querySpec">An instance of the <see cref="IQuerySpec{TRow}"/> interface.</param>
        /// <param name="timeout">How long to wait for the query to complete.</param>
        /// <param name="pollFrequency">Optional. How often to poll the api endpoint. Defaults to
        /// DefaultPollingFrequencyMillis.</param>
        /// <returns>A <see cref="QueryResult{TRow}"/> instance containing the success status and query Id.</returns>
        /// <typeparam name="TRow">The type that the query row will be deserialized to.</typeparam>
        public async Task<QueryResult<TRow>> Query<TRow>(
            string dataSource,
            IQuerySpec<TRow> querySpec,
            TimeSpan timeout,
            TimeSpan? pollFrequency = null)
        {
            var queryResponse = await SubmitQuery(dataSource, querySpec.QueryStatement);
            if (!queryResponse.Success)
            {
                throw new Exception($"Unhandled Aircloak error returned for query to {dataSource}. Query Statement:" +
                                    $" {querySpec.QueryStatement}.");
            }

            return await PollQueryUntilCompleteOrTimeout(queryResponse.QueryId, querySpec, timeout, pollFrequency);
        }

        /// <summary>
        /// Sends a Http POST request to the Aircloak server's /api/queries endpoint.
        /// </summary>
        /// <param name="dataSource">The data source to run the query against.</param>
        /// <param name="queryStatement">The query statement as a string.</param>
        /// <returns>A QueryResonse instance containing the success status and query Id.</returns>
        public async Task<QueryResponse> SubmitQuery(
            string dataSource,
            string queryStatement)
        {
            var queryBody = new
            {
                query = new
                {
                    statement = queryStatement,
                    data_source_name = dataSource,
                },
            };

            return await ApiPostRequest<QueryResponse>("queries", JsonSerializer.Serialize(queryBody));
        }

        /// <summary>
        /// Sends a Http POST request to the Aircloak server's /api/queries/{query_id} endpoint.
        /// </summary>
        /// <param name="queryId">The query Id obtained via a previous call to the /api/query endpoint.</param>
        /// <param name="querySpec">An instance of the <see cref="IQuerySpec{TRow}"/> interface.</param>
        /// <typeparam name="TRow">The type to use to deserialise each row returned in the query results.</typeparam>
        /// <returns>A QueryResult instance. If the query has finished executing, contains the query results, with each
        /// row seralised to type <c>TRow</c>.</returns>
        public async Task<QueryResult<TRow>> PollQueryResult<TRow>(string queryId, IQuerySpec<TRow> querySpec)
        {
            // Register the JsonArrayConverter so the TRow can be deserialized correctly
            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = new SnakeCaseNamingPolicy(),
                Converters =
                {
                    new JsonArrayConverter<IQuerySpec<TRow>, TRow>(querySpec),
                },
            };

            return await ApiGetRequest<QueryResult<TRow>>($"queries/{queryId}", jsonOptions);
        }

        /// <summary>
        /// Calls <c>PollQueryResult</c> in a loop until the query completes. Can be canceled.
        /// </summary>
        /// <remarks>
        /// Canceling via the <c>CancellationToken</c> cancels the returned <c>Task</c> but does not
        /// cancel query execution. To do this, a call to <c>/api/queries/{query_id}/cancel</c> must be made.
        /// </remarks>
        /// <param name="queryId">The query Id obtained via a previous call to the /api/query endpoint.</param>
        /// <param name="querySpec">An instance of the <see cref="IQuerySpec{TRow}"/> interface.</param>
        /// <param name="ct">A <c>CancellationToken</c> that cancels the returned <c>Task</c>.</param>
        /// <param name="pollFrequency">How often to poll the api endpoint. Default is every
        /// DefaultPollingFrequencyMillis milliseconds.
        /// </param>
        /// <typeparam name="TRow">The type to use to deserialise each row returned in the query results.</typeparam>
        /// <returns>A QueryResult instance. If the query has finished executing, contains the query results, with each
        /// row seralised to type <c>TRow</c>.</returns>
        public async Task<QueryResult<TRow>> PollQueryUntilComplete<TRow>(
            string queryId,
            IQuerySpec<TRow> querySpec,
            CancellationToken ct,
            TimeSpan? pollFrequency = null)
        {
            if (pollFrequency is null)
            {
                pollFrequency = TimeSpan.FromMilliseconds(DefaultPollingFrequencyMillis);
            }

            while (true)
            {
                if (ct.IsCancellationRequested)
                {
                    await CancelQuery(queryId);
                    ct.ThrowIfCancellationRequested();
                }

                // Note: the cancellation token is not currently passed through to the PollQueryResult call. The
                // assumption here is that polling should return a result immediately. In practice, this may not be the
                // case due to network delay. In this case, cancellation requests may take longer to take effect than
                // expected. In future we could thread the cancellation token right down through to the raw SyncSend
                // call in the HttpClient.
                var queryResult = await PollQueryResult<TRow>(queryId, querySpec);
                if (queryResult.Query.Completed)
                {
                    return queryResult;
                }
                else
                {
                    await Task.Delay(pollFrequency.Value, ct);
                }
            }
        }

        /// <summary>
        /// Polls for a query's results until query resolution is complete, or until a specified timeout.
        /// </summary>
        /// <param name="queryId">The query Id obtained via a previous call to the /api/query endpoint.</param>
        /// <param name="querySpec">An instance of the <see cref="IQuerySpec{TRow}"/> interface.</param>
        /// <param name="timeout">How long to wait for the query to complete.</param>
        /// <param name="pollFrequency">Optional. How often to poll the api endpoint. Defaults to
        /// DefaultPollingFrequencyMillis.</param>
        /// <typeparam name="TRow">The type to use to deserialise each row returned in the query results.</typeparam>
        /// <returns>A QueryResult instance. If the query has finished executing, contains the query results, with each
        /// row seralised to type <c>TRow</c>.</returns>
        public async Task<QueryResult<TRow>> PollQueryUntilCompleteOrTimeout<TRow>(
            string queryId,
            IQuerySpec<TRow> querySpec,
            TimeSpan timeout,
            TimeSpan? pollFrequency = null)
        {
            using var tokenSource = new CancellationTokenSource();
            tokenSource.CancelAfter(timeout);

            try
            {
                return await PollQueryUntilComplete<TRow>(queryId, querySpec, tokenSource.Token, pollFrequency);
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
        /// <param name="queryId">The id of the query to cancel.</param>
        /// <returns>A <c>CancelResponse</c> instance indicating whether or not the query was indeed canceled.
        /// </returns>
        public async Task<CancelResponse> CancelQuery(string queryId)
        {
            return await ApiPostRequest<CancelResponse>($"queries/{queryId}/cancel");
        }

        /// <summary>
        /// Send a GET request to the Aircloak API. Handles authentication.
        /// </summary>
        /// <param name="apiEndpoint">The API endpoint to target.</param>
        /// <param name="options">Overrides the default <c>JsonSerializerOptions</c>.</param>
        /// <typeparam name="T">The type to deserialize the JSON response to.</typeparam>
        /// <returns>A <c>Task&lt;T&gt;</c> which, upon completion, contains the API response deserialized
        /// to the provided return type.</returns>
        /// <exception cref="HttpRequestException">The request failed due to an underlying issue
        /// such as network connectivity, DNS failure, server certificate validation or timeout.
        /// </exception>
        /// <exception cref="JsonException">The JSON is invalid.
        /// -or- <c>T</c> is not compatible with the JSON.
        /// -or- There is remaining data in the stream.
        /// </exception>
        public async Task<T> ApiGetRequest<T>(
            string apiEndpoint,
            JsonSerializerOptions? options = null)
        {
            return await ApiRequest<T>(HttpMethod.Get, apiEndpoint, options: options);
        }

        /// <summary>
        /// Send a POST request to the Aircloak API. Handles authentication.
        /// </summary>
        /// <param name="apiEndpoint">The API endpoint to target.</param>
        /// <param name="requestContent">JSON-encoded request message (optional).</param>
        /// <param name="options">Overrides the default <c>JsonSerializerOptions</c>.</param>
        /// <typeparam name="T">The type to deserialize the JSON response to.</typeparam>
        /// <returns>A <c>Task&lt;T&gt;</c> which, upon completion, contains the API response deserialized
        /// to the provided return type.</returns>
        /// <exception cref="HttpRequestException">The request failed due to an underlying issue
        /// such as network connectivity, DNS failure, server certificate validation or timeout.
        /// </exception>
        /// <exception cref="JsonException">The JSON is invalid.
        /// -or- <c>T</c> is not compatible with the JSON.
        /// -or- There is remaining data in the stream.
        /// </exception>
        public async Task<T> ApiPostRequest<T>(
            string apiEndpoint,
            string? requestContent = default,
            JsonSerializerOptions? options = null)
        {
            return await ApiRequest<T>(HttpMethod.Post, apiEndpoint, requestContent, options);
        }

        /// <summary>
        /// Turns the HTTP response into a custom error string.
        /// </summary>
        /// <param name="response">The HTTP response.</param>
        /// <returns>A string containing a custom error message.</returns>
        private static string ServiceError(HttpResponseMessage response)
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

        /// <summary>
        /// Send a request to the Aircloak API. Handles authentication.
        /// </summary>
        /// <param name="requestMethod">The HTTP method to use in the request.</param>
        /// <param name="apiEndpoint">The API endpoint to target.</param>
        /// <param name="requestContent">JSON-encoded request message (optional).</param>
        /// <param name="options">Overrides the default <c>JsonSerializerOptions</c>.</param>
        /// <typeparam name="T">The type to deserialize the JSON response to.</typeparam>
        /// <returns>A <c>Task&lt;T&gt;</c> which, upon completion, contains the API response deserialized
        /// to the provided return type.</returns>
        /// <exception cref="HttpRequestException">The request failed due to an underlying issue
        /// such as network connectivity, DNS failure, server certificate validation or timeout.
        /// </exception>
        /// <exception cref="JsonException">The JSON is invalid.
        /// -or- <c>T</c> is not compatible with the JSON.
        /// -or- There is remaining data in the stream.
        /// </exception>
        private async Task<T> ApiRequest<T>(
            HttpMethod requestMethod,
            string apiEndpoint,
            string? requestContent = default,
            JsonSerializerOptions? options = null)
        {
            using var requestMessage =
                new HttpRequestMessage(requestMethod, apiEndpoint);

            if (!(requestContent is null))
            {
                requestMessage.Content = new StringContent(requestContent);
                requestMessage.Content.Headers.ContentType = new MediaTypeHeaderValue(System.Net.Mime.MediaTypeNames.Application.Json);
            }

            using var response = await httpClient.SendAsync(
                requestMessage,
                HttpCompletionOption.ResponseHeadersRead);

            if (response.IsSuccessStatusCode)
            {
                using var contentStream = await response.Content.ReadAsStreamAsync();
                return await JsonSerializer.DeserializeAsync<T>(
                    contentStream,
                    options ?? DefaultJsonOptions);
            }
            else
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException($"Request Error: {ServiceError(response)}.\n{requestMessage}\n{requestContent}\n{responseContent}");
            }
        }
    }
}
