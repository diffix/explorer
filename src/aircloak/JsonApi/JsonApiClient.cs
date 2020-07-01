namespace Aircloak.JsonApi
{
    using System;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;

    using Aircloak.JsonApi.JsonConversion;
    using Aircloak.JsonApi.ResponseTypes;
    using Diffix;
    using Diffix.JsonConversion;

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
        internal const string HttpClientName = "aircloak-api";

        private static readonly JsonSerializerOptions DefaultJsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = new SnakeCaseNamingPolicy(),
            Converters = { new DValueTypeEnumConverter() },
        };

        private readonly HttpClient httpClient;

        private readonly IAircloakAuthenticationProvider authProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonApiClient" /> class.
        /// </summary>
        /// <param name="httpClientFactory">A HttpClient factory providing a named client <see ref="HttpClientName" />.
        /// </param>
        /// <param name="authProvider">An authentication token source.</param>
        public JsonApiClient(IHttpClientFactory httpClientFactory, IAircloakAuthenticationProvider authProvider)
        {
            httpClient = httpClientFactory.CreateClient(HttpClientName);
            this.authProvider = authProvider;
        }

        /// <summary>
        /// Sends a Http GET request to the Aircloak server's /api/data_sources endpoint.
        /// </summary>
        /// <param name="apiUrl">The Url of the Aircloak api.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken" /> object that can be used to cancel the operation.</param>
        /// <returns>A <c>List&lt;DataSource&gt;</c> containing the data sources provided by this
        /// Aircloak instance.</returns>
        public async Task<DataSourceCollection> GetDataSources(
            Uri apiUrl,
            CancellationToken cancellationToken)
        {
            var endPoint = new Uri(apiUrl, "data_sources");
            using var httpResponse = await ApiGetRequest(endPoint, cancellationToken);
            return await ParseJson<DataSourceCollection>(httpResponse, DefaultJsonOptions, cancellationToken);
        }

        /// <summary>
        /// Posts a query to the Aircloak server, retrieves the query ID, and then polls for the result.
        /// </summary>
        /// <param name="apiUrl">The Url of the Aircloak api.</param>
        /// <param name="dataSource">The data source to run the query against.</param>
        /// <param name="query">An instance of the <see cref="DQuery{TRow}"/> interface.</param>
        /// <param name="pollFrequency">How often to poll the api endpoint. </param>
        /// <param name="cancellationToken">A <see cref="CancellationToken" /> object that can be used to cancel the operation.</param>
        /// <returns>A <see cref="QueryResult{TRow}"/> instance containing the success status and query Id.</returns>
        /// <typeparam name="TRow">The type that the query row will be deserialized to.</typeparam>
        public async Task<QueryResult<TRow>> Query<TRow>(
            Uri apiUrl,
            string dataSource,
            DQuery<TRow> query,
            TimeSpan pollFrequency,
            CancellationToken cancellationToken)
        {
            var queryResponse = await SubmitQuery(apiUrl, dataSource, query.QueryStatement, cancellationToken);
            return await PollQueryUntilComplete(apiUrl, queryResponse.QueryId, query, pollFrequency, cancellationToken);
        }

        /// <summary>
        /// Sends a Http POST request to the Aircloak server's /api/queries endpoint.
        /// </summary>
        /// <param name="apiUrl">The Url of the Aircloak api.</param>
        /// <param name="dataSource">The data source to run the query against.</param>
        /// <param name="queryStatement">The query statement as a string.</param>
        /// <param name="cancellationToken">A <c>CancellationToken</c> that cancels the returned <c>Task</c>.</param>
        /// <returns>A QueryResonse instance containing the success status and query Id.</returns>
        public async Task<QueryResponse> SubmitQuery(
            Uri apiUrl,
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

            var endPoint = new Uri(apiUrl, "queries");
            var requestContent = JsonSerializer.Serialize(queryBody);
            using var httpResponse = await ApiPostRequest(endPoint, requestContent, cancellationToken);
            var queryResponse = await ParseJson<QueryResponse>(httpResponse, DefaultJsonOptions, cancellationToken);

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
        /// <param name="apiUrl">The Url of the Aircloak api.</param>
        /// <param name="queryId">The query Id obtained via a previous call to the /api/query endpoint.</param>
        /// <param name="query">An instance of the <see cref="DQuery{TRow}"/> interface.</param>
        /// <param name="pollFrequency">How often to poll the api endpoint. </param>
        /// <param name="cancellationToken">A <c>CancellationToken</c> that cancels the returned <c>Task</c>.</param>
        /// <typeparam name="TRow">The type to use to deserialise each row returned in the query results.</typeparam>
        /// <returns>A QueryResult instance. If the query has finished executing, contains the query results, with each
        /// row seralised to type <c>TRow</c>.</returns>
        public async Task<QueryResult<TRow>> PollQueryUntilComplete<TRow>(
            Uri apiUrl,
            string queryId,
            DQuery<TRow> query,
            TimeSpan pollFrequency,
            CancellationToken cancellationToken)
        {
            // Register the JsonArrayConverter so the TRow can be deserialized correctly
            var jsonDeserializeOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = new SnakeCaseNamingPolicy(),
                Converters =
                {
                    new JsonArrayConverter<DQuery<TRow>, TRow>(query),
                    new DValueTypeEnumConverter(),
                },
            };

            var queryCompleted = false;
            var speed = 16;

            try
            {
                while (true)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var endPoint = new Uri(apiUrl, $"queries/{queryId}");
                    using var httpResponse = await ApiGetRequest(endPoint, cancellationToken);
                    var queryResult = await ParseJson<QueryResult<TRow>>(httpResponse, jsonDeserializeOptions, cancellationToken);

                    if (queryResult.Query.Completed)
                    {
                        queryCompleted = true;
                        switch (queryResult.Query.QueryState)
                        {
                            case "completed":
                                return queryResult;
                            case "error":
                                throw new Exception("Aircloak API query error.\n" +
                                    GetQueryResultDetails(query, queryResult));
                            case "cancelled":
                                throw new OperationCanceledException("Aircloak API query canceled.\n" +
                                    GetQueryResultDetails(query, queryResult));
                        }
                    }

                    await Task.Delay(pollFrequency / speed, cancellationToken);
                    speed = Math.Max(speed / 2, 1);
                }
            }
            catch (Exception)
            {
                if (!queryCompleted)
                {
                    await CancelQuery(apiUrl, queryId);
                }
                throw;
            }

            static string GetQueryResultDetails(DQuery<TRow> query, QueryResult<TRow> queryResult)
            {
                return $"DataSource: {queryResult.Query.DataSource.Name}.\n" +
                    $"Error: {queryResult.Query.Error}\n" +
                    $"Query Id: {queryResult.Query.Id}\n" +
                    $"Query Statement: {query.QueryStatement}";
            }
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
        /// Sends a Http GET request to the /api/queries/{query_id}/cancel. Cancels a running query on the Aircloak
        /// server.
        /// </summary>
        /// <param name="apiUrl">The Url of the Aircloak api.</param>
        /// <param name="queryId">The id of the query to cancel.</param>
        /// <returns>A <c>CancelResponse</c> instance indicating whether or not the query was indeed canceled.
        /// </returns>
        public async Task<CancelResponse> CancelQuery(
            Uri apiUrl,
            string queryId)
        {
            var endPoint = new Uri(apiUrl, $"queries/{queryId}/cancel");
            using var httpResponse = await ApiPostRequest(endPoint, null, CancellationToken.None);
            return await ParseJson<CancelResponse>(httpResponse, DefaultJsonOptions, CancellationToken.None);
        }

        /// <summary>
        /// Send a GET request to the Aircloak API. Handles authentication.
        /// </summary>
        /// <param name="apiEndpoint">The API endpoint to target.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken" /> object that can be used to cancel the operation.</param>
        /// <returns>A <c>Task&lt;HttpResponseMessage&gt;</c> which, upon completion, contains the API response.</returns>
        /// <exception cref="HttpRequestException">The request failed due to an underlying issue
        /// such as network connectivity, DNS failure, server certificate validation or timeout.
        /// </exception>
        private async Task<HttpResponseMessage> ApiGetRequest(
            Uri apiEndpoint,
            CancellationToken cancellationToken)
        {
            return await ApiRequest(HttpMethod.Get, apiEndpoint, null, cancellationToken);
        }

        /// <summary>
        /// Send a POST request to the Aircloak API. Handles authentication.
        /// </summary>
        /// <param name="apiEndpoint">The API endpoint to target.</param>
        /// <param name="requestContent">JSON-encoded request message (optional).</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken" /> object that can be used to cancel the operation.</param>
        /// <returns>A <c>Task&lt;HttpResponseMessage&gt;</c> which, upon completion, contains the API response.</returns>
        /// <exception cref="HttpRequestException">The request failed due to an underlying issue
        /// such as network connectivity, DNS failure, server certificate validation or timeout.
        /// </exception>
        private async Task<HttpResponseMessage> ApiPostRequest(
            Uri apiEndpoint,
            string? requestContent,
            CancellationToken cancellationToken)
        {
            return await ApiRequest(HttpMethod.Post, apiEndpoint, requestContent, cancellationToken);
        }

        /// <summary>
        /// Send a request to the Aircloak API. Handles authentication.
        /// </summary>
        /// <param name="requestMethod">The HTTP method to use in the request.</param>
        /// <param name="apiEndpoint">The API endpoint to target.</param>
        /// <param name="requestContent">JSON-encoded request message (optional).</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken" /> object that can be used to cancel the operation.</param>
        /// <returns>A <c>Task&lt;HttpResponseMessage&gt;</c> which, upon completion, contains the API response.</returns>
        /// <exception cref="HttpRequestException">The request failed due to an underlying issue
        /// such as network connectivity, DNS failure, server certificate validation or timeout.
        /// </exception>
        private async Task<HttpResponseMessage> ApiRequest(
            HttpMethod requestMethod,
            Uri apiEndpoint,
            string? requestContent,
            CancellationToken cancellationToken)
        {
            using var requestMessage = new HttpRequestMessage(requestMethod, apiEndpoint);

            if (!(requestContent is null))
            {
                requestMessage.Content = new StringContent(requestContent);
                requestMessage.Content.Headers.ContentType = new MediaTypeHeaderValue(System.Net.Mime.MediaTypeNames.Application.Json);
            }

            var authToken = await authProvider.GetAuthToken();
            if (!requestMessage.Headers.TryAddWithoutValidation("auth-token", authToken))
            {
                throw new Exception("Failed to add auth-token header!");
            }

            var response = await httpClient.SendAsync(
                requestMessage,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException($"Request Error: {ServiceError(response)}.\n{requestMessage}\n{requestContent}\n{responseContent}");
            }

            return response;
        }

        /// <summary>
        /// Parses JSON data into an object.
        /// </summary>
        /// <param name="httpResponse">A <see cref="HttpResponseMessage" /> object containing the JSON data to be parsed.</param>
        /// <param name="options">Overrides the default <c>JsonSerializerOptions</c>.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken" /> object that can be used to cancel the operation.</param>
        /// <typeparam name="T">The type to deserialize the JSON response to.</typeparam>
        /// <returns>A <c>Task&lt;T&gt;</c> which, upon completion, contains the API response deserialized
        /// to the provided return type.</returns>
        /// <exception cref="JsonException">The JSON is invalid.
        /// -or- <c>T</c> is not compatible with the JSON.
        /// -or- There is remaining data in the stream.
        /// </exception>
        private static async Task<T> ParseJson<T>(HttpResponseMessage httpResponse, JsonSerializerOptions options, CancellationToken cancellationToken)
        {
            var stream = await httpResponse.Content.ReadAsStreamAsync();
            return await JsonSerializer.DeserializeAsync<T>(stream, options, cancellationToken);
        }
    }
}
