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
        private static readonly JsonSerializerOptions DefaultJsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = new SnakeCaseNamingPolicy(),
        };

        private readonly HttpClient httpClient;

        private readonly IAircloakAuthenticationProvider authProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonApiClient" /> class.
        /// </summary>
        /// <param name="httpClient">A HttpClient object injected into this instance.</param>
        /// <param name="authProvider">An authentication token source.</param>
        public JsonApiClient(HttpClient httpClient, IAircloakAuthenticationProvider authProvider)
        {
            this.httpClient = httpClient;
            this.authProvider = authProvider;
        }

        /// <summary>
        /// Sends a Http GET request to the Aircloak server's /api/data_sources endpoint.
        /// </summary>
        /// <param name="cancellationToken">A <see cref="CancellationToken" /> object that can be used to cancel the operation.</param>
        /// <returns>A <c>List&lt;DataSource&gt;</c> containing the data sources provided by this
        /// Aircloak instance.</returns>
        public async Task<DataSourceCollection> GetDataSources(CancellationToken cancellationToken)
        {
            return await ApiGetRequest<DataSourceCollection>("data_sources", DefaultJsonOptions, cancellationToken);
        }

        /// <summary>
        /// Posts a query to the Aircloak server, retrieves the query ID, and then polls for the result.
        /// </summary>
        /// <param name="dataSource">The data source to run the query against.</param>
        /// <param name="querySpec">An instance of the <see cref="IQuerySpec{TRow}"/> interface.</param>
        /// <param name="pollFrequency">How often to poll the api endpoint. </param>
        /// <param name="cancellationToken">A <see cref="CancellationToken" /> object that can be used to cancel the operation.</param>
        /// <returns>A <see cref="QueryResult{TRow}"/> instance containing the success status and query Id.</returns>
        /// <typeparam name="TRow">The type that the query row will be deserialized to.</typeparam>
        public async Task<QueryResult<TRow>> Query<TRow>(
            string dataSource,
            IQuerySpec<TRow> querySpec,
            TimeSpan pollFrequency,
            CancellationToken cancellationToken)
        {
            var queryResponse = await SubmitQuery(dataSource, querySpec.QueryStatement, cancellationToken);
            return await PollQueryUntilComplete(queryResponse.QueryId, querySpec, pollFrequency, cancellationToken);
        }

        /// <summary>
        /// Sends a Http POST request to the Aircloak server's /api/queries endpoint.
        /// </summary>
        /// <param name="dataSource">The data source to run the query against.</param>
        /// <param name="queryStatement">The query statement as a string.</param>
        /// <param name="cancellationToken">A <c>CancellationToken</c> that cancels the returned <c>Task</c>.</param>
        /// <returns>A QueryResonse instance containing the success status and query Id.</returns>
        public async Task<QueryResponse> SubmitQuery(
            string dataSource,
            string queryStatement,
            CancellationToken cancellationToken)
        {
            var queryBody = new
            {
                query = new
                {
                    statement = queryStatement,
                    data_source_name = dataSource,
                },
            };
            var requestContent = JsonSerializer.Serialize(queryBody);
            var queryResponse = await ApiPostRequest<QueryResponse>(
                "queries", requestContent, DefaultJsonOptions, cancellationToken);
            if (!queryResponse.Success)
            {
                throw new Exception($"Unhandled Aircloak error returned for query to {dataSource}. Query Statement:" +
                                    $" {queryStatement}.");
            }
            return queryResponse;
        }

        /// <summary>
        /// Calls <c>PollQueryResult</c> in a loop until the query completes. Can be canceled.
        /// </summary>
        /// <remarks>
        /// Canceling via the <c>CancellationToken</c> cancels the returned <c>Task</c> and
        /// automatically cancels the query execution by calling <c>/api/queries/{query_id}/cancel</c>.
        /// </remarks>
        /// <param name="queryId">The query Id obtained via a previous call to the /api/query endpoint.</param>
        /// <param name="querySpec">An instance of the <see cref="IQuerySpec{TRow}"/> interface.</param>
        /// <param name="pollFrequency">How often to poll the api endpoint. </param>
        /// <param name="cancellationToken">A <c>CancellationToken</c> that cancels the returned <c>Task</c>.</param>
        /// <typeparam name="TRow">The type to use to deserialise each row returned in the query results.</typeparam>
        /// <returns>A QueryResult instance. If the query has finished executing, contains the query results, with each
        /// row seralised to type <c>TRow</c>.</returns>
        public async Task<QueryResult<TRow>> PollQueryUntilComplete<TRow>(
            string queryId,
            IQuerySpec<TRow> querySpec,
            TimeSpan pollFrequency,
            CancellationToken cancellationToken)
        {
            // Register the JsonArrayConverter so the TRow can be deserialized correctly
            var jsonDeserializeOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = new SnakeCaseNamingPolicy(),
                Converters = { new JsonArrayConverter<IQuerySpec<TRow>, TRow>(querySpec) },
            };

            try
            {
                while (true)
                {
                    var queryResult = await ApiGetRequest<QueryResult<TRow>>(
                        $"queries/{queryId}", jsonDeserializeOptions, cancellationToken);

                    if (queryResult.Query.Completed)
                    {
                        switch (queryResult.Query.QueryState)
                        {
                            case "completed":
                                return queryResult;
                            case "error":
                                throw new Exception("Aircloak API query error.\n" +
                                    GetQueryResultDetails(querySpec, queryResult));
                            case "cancelled":
                                throw new OperationCanceledException("Aircloak API query canceled.\n" +
                                    GetQueryResultDetails(querySpec, queryResult));
                        }
                    }

                    await Task.Delay(pollFrequency, cancellationToken);
                    cancellationToken.ThrowIfCancellationRequested();
                }
            }
            catch (OperationCanceledException ex)
            {
                if (ex.CancellationToken == cancellationToken)
                {
                    await CancelQuery(queryId, cancellationToken);
                }
                throw;
            }

            static string GetQueryResultDetails(IQuerySpec<TRow> querySpec, QueryResult<TRow> queryResult)
            {
                return $"DataSource: {queryResult.Query.DataSource}.\n" +
                    $"Error: {queryResult.Query.Error}\n" +
                    $"Query Statement: {querySpec.QueryStatement}";
            }
        }

        /// <summary>
        /// Sends a Http GET request to the /api/queries/{query_id}/cancel. Cancels a running query on the Aircloak
        /// server.
        /// </summary>
        /// <param name="queryId">The id of the query to cancel.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken" /> object that can be used to cancel the operation.</param>
        /// <returns>A <c>CancelResponse</c> instance indicating whether or not the query was indeed canceled.
        /// </returns>
        public async Task<CancelResponse> CancelQuery(string queryId, CancellationToken cancellationToken)
        {
            return await ApiPostRequest<CancelResponse>(
                $"queries/{queryId}/cancel", null, DefaultJsonOptions, cancellationToken);
        }

        /// <summary>
        /// Send a GET request to the Aircloak API. Handles authentication.
        /// </summary>
        /// <param name="apiEndpoint">The API endpoint to target.</param>
        /// <param name="options">Overrides the default <c>JsonSerializerOptions</c>.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken" /> object that can be used to cancel the operation.</param>
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
        private async Task<T> ApiGetRequest<T>(
            string apiEndpoint,
            JsonSerializerOptions options,
            CancellationToken cancellationToken)
        {
            return await ApiRequest<T>(HttpMethod.Get, apiEndpoint, null, options, cancellationToken);
        }

        /// <summary>
        /// Send a POST request to the Aircloak API. Handles authentication.
        /// </summary>
        /// <param name="apiEndpoint">The API endpoint to target.</param>
        /// <param name="requestContent">JSON-encoded request message (optional).</param>
        /// <param name="options">Overrides the default <c>JsonSerializerOptions</c>.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken" /> object that can be used to cancel the operation.</param>
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
        private async Task<T> ApiPostRequest<T>(
            string apiEndpoint,
            string? requestContent,
            JsonSerializerOptions options,
            CancellationToken cancellationToken)
        {
            return await ApiRequest<T>(HttpMethod.Post, apiEndpoint, requestContent, options, cancellationToken);
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
        /// <param name="cancellationToken">A <see cref="CancellationToken" /> object that can be used to cancel the operation.</param>
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
            string? requestContent,
            JsonSerializerOptions options,
            CancellationToken cancellationToken)
        {
            using var requestMessage = new HttpRequestMessage(requestMethod, apiEndpoint);

            if (!(requestContent is null))
            {
                requestMessage.Content = new StringContent(requestContent);
                requestMessage.Content.Headers.ContentType = new MediaTypeHeaderValue(System.Net.Mime.MediaTypeNames.Application.Json);
            }

            var authToken = await Task.Run(authProvider.GetAuthToken, cancellationToken);
            if (!requestMessage.Headers.TryAddWithoutValidation("auth-token", authToken))
            {
                throw new Exception("Failed to add auth-token header!");
            }

            using var response = await httpClient.SendAsync(
                requestMessage,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                using var contentStream = await Task.Run(response.Content.ReadAsStreamAsync, cancellationToken);
                return await JsonSerializer.DeserializeAsync<T>(
                    contentStream,
                    options,
                    cancellationToken);
            }
            else
            {
                var responseContent = await Task.Run(response.Content.ReadAsStringAsync, cancellationToken);
                throw new HttpRequestException($"Request Error: {ServiceError(response)}.\n{requestMessage}\n{requestContent}\n{responseContent}");
            }
        }
    }
}
