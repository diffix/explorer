namespace Aircloak.JsonApi
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;

    using Aircloak.JsonApi.ResponseTypes;

    /// <summary>
    /// Provides higher-level access to the Aircloak Http Api. The session uses the same Url root and Api Key
    /// throughout its lifetime.
    /// </summary>
    /// <see cref="JsonApiClient"/>
    public class JsonApiSession
    {
        private const int DefaultPollingFrequencyMillis = 2000;
        private readonly JsonApiClient apiClient;
        private readonly Uri apiRootUrl;
        private readonly string apiKey;

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonApiSession"/> class.
        /// </summary>
        /// <param name="apiClient">An <c>JsonApiClient</c> instance. The <paramref name="apiClient"/> is based on
        /// the .NET <c>HttpClient</c> class and should be reused, ie. the <paramref name="apiClient"/> should be a
        /// reference to singleton instance.</param>
        /// <param name="apiRootUrl">The root Url for the Aircloak Api, eg. "https://attack.aircloak.com/api/".</param>
        /// <param name="apiKey">The Api key to use for this session.</param>
        /// <see cref="JsonApiClient"/>
        public JsonApiSession(
            JsonApiClient apiClient,
            Uri apiRootUrl,
            string apiKey)
        {
            this.apiClient = apiClient;
            this.apiKey = apiKey;
            this.apiRootUrl = apiRootUrl;
        }

        /// <summary>
        /// Sends a Http GET request to the Aircloak server's /api/data_sources endpoint.
        /// </summary>
        /// <returns>A <c>List&lt;DataSource&gt;</c> containing the data sources provided by this
        /// Aircloak instance.</returns>
        public async Task<List<DataSource>> GetDataSources()
        {
            return await apiClient.ApiGetRequest<List<DataSource>>(
                new Uri(apiRootUrl, "data_sources"),
                apiKey);
        }

        /// <summary>
        /// Posts a query to the Aircloak server, retrieves the query ID, and then polls for the result.
        /// </summary>
        /// <param name="dataSource">The data source to run the query against.</param>
        /// <param name="queryStatement">The query statement as a string.</param>
        /// <param name="timeout">How long to wait for the query to complete.</param>
        /// <returns>A <see cref="QueryResult{TRow}"/> instance containing the success status and query Id.</returns>
        /// <typeparam name="TRow">The type that the query row will be deserialized to.</typeparam>
        public async Task<QueryResult<TRow>> Query<TRow>(
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

            return await PollQueryUntilCompleteOrTimeout<TRow>(queryResponse.QueryId, timeout);
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

            return await apiClient.ApiPostRequest<QueryResponse>(
                EndPointUrl("queries"),
                apiKey,
                JsonSerializer.Serialize(queryBody));
        }

        /// <summary>
        /// Sends a Http POST request to the Aircloak server's /api/queries/{query_id} endpoint.
        /// </summary>
        /// <param name="queryId">The query Id obtained via a previous call to the /api/query endpoint.</param>
        /// <typeparam name="TRow">The type to use to deserialise each row returned in the query results.</typeparam>
        /// <returns>A QueryResult instance. If the query has finished executing, contains the query results, with each
        /// row seralised to type <c>TRow</c>.</returns>
        public async Task<QueryResult<TRow>> PollQueryResult<TRow>(string queryId)
        {
            return await apiClient.ApiGetRequest<QueryResult<TRow>>(
                EndPointUrl($"queries/{queryId}"),
                apiKey);
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
        /// <param name="pollFrequency">How often to poll the api endpoint. Default is every
        /// DefaultPollingFrequencyMillis milliseconds.
        /// </param>
        /// <typeparam name="TRow">The type to use to deserialise each row returned in the query results.</typeparam>
        /// <returns>A QueryResult instance. If the query has finished executing, contains the query results, with each
        /// row seralised to type <c>TRow</c>.</returns>
        public async Task<QueryResult<TRow>> PollQueryUntilComplete<TRow>(
            string queryId,
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
                var queryResult = await PollQueryResult<TRow>(queryId);
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
        /// <param name="timeout">How long to wait for the query to complete.</param>
        /// <param name="pollFrequency">Optional. How often to poll the api endpoint. Defaults to
        /// DefaultPollingFrequencyMillis.</param>
        /// <typeparam name="TRow">The type to use to deserialise each row returned in the query results.</typeparam>
        /// <returns>A QueryResult instance. If the query has finished executing, contains the query results, with each
        /// row seralised to type <c>TRow</c>.</returns>
        public async Task<QueryResult<TRow>> PollQueryUntilCompleteOrTimeout<TRow>(
            string queryId,
            TimeSpan timeout,
            TimeSpan? pollFrequency = null)
        {
            using var tokenSource = new CancellationTokenSource();
            tokenSource.CancelAfter(timeout);

            try
            {
                return await PollQueryUntilComplete<TRow>(queryId, tokenSource.Token, pollFrequency);
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
            return await apiClient.ApiPostRequest<CancelResponse>(
                EndPointUrl($"queries/{queryId}/cancel"),
                apiKey);
        }

        private Uri EndPointUrl(string path)
        {
            return new Uri(apiRootUrl, path);
        }
    }
}
