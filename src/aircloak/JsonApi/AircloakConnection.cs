namespace Aircloak.JsonApi
{
    using System;
    using System.Collections.Concurrent;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;

    using Aircloak.JsonApi.ResponseTypes;
    using Diffix;
    using Microsoft.Extensions.Options;

    /// <summary>
    /// Parses a row instance.
    /// </summary>
    /// <param name="reader">The <see cref="Utf8JsonReader"/> instance to use for parsing the result.</param>
    /// <typeparam name="TRow">The type of the rows returned by the query.</typeparam>
    /// <returns>The parsed value.</returns>
    public delegate TRow JsonRowParser<TRow>(ref Utf8JsonReader reader);

    /// <summary>
    /// Defines a connection to the Aircloak system that can be used for executing queries.
    /// </summary>
    public class AircloakConnection
    {
        private static readonly ConcurrentDictionary<Uri, SemaphoreSlim> Semaphores =
            new ConcurrentDictionary<Uri, SemaphoreSlim>();

        private readonly string dataSourceName;
        private readonly Uri apiUrl;
        private readonly JsonApiClient apiClient;
        private readonly TimeSpan pollFrequency;
        private readonly CancellationToken cancellationToken;
        private readonly SemaphoreSlim semaphore;

        /// <summary>
        /// Initializes a new  instance of the <see cref="AircloakConnection" /> class.
        /// </summary>
        /// <param name="apiClient">The helper API client object which will be used to make HTTP requests to the Aircloak system.</param>
        /// <param name="apiUrl">The base url of the Aircloak Api.</param>
        /// <param name="dataSourceName">The data source to run the queries against.</param>
        /// <param name="options">The connection options.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken" /> that signals the query to abort.</param>
        public AircloakConnection(
            JsonApiClient apiClient,
            Uri apiUrl,
            string dataSourceName,
            IOptions<ConnectionOptions> options,
            CancellationToken cancellationToken)
        {
            this.apiUrl = apiUrl;
            this.apiClient = apiClient;
            this.dataSourceName = dataSourceName;
            this.cancellationToken = cancellationToken;

            pollFrequency = TimeSpan.FromMilliseconds(options.Value.PollingInterval);
            semaphore = Semaphores.GetOrAdd(apiUrl, _ => new SemaphoreSlim(options.Value.MaxConcurrentQueries));
        }

        /// <summary>
        /// Executes the query.
        /// </summary>
        /// <param name="query">An object defining the query to be executed.</param>
        /// <param name="rowParser">A delegate used for parsing a result row.</param>
        /// <typeparam name="TRow">The type of the rows returned by the query.</typeparam>
        /// <returns>An object containing a collection with the rows returned by the query.</returns>
        public async Task<DResult<TRow>> Exec<TRow>(string query, JsonRowParser<TRow> rowParser)
        {
            await semaphore.WaitAsync(cancellationToken);

            try
            {
                return await apiClient.Query(
                    apiUrl,
                    dataSourceName,
                    query,
                    rowParser,
                    pollFrequency,
                    cancellationToken);
            }
            finally
            {
                semaphore.Release();
            }
        }

        /// <summary>
        /// Get the list of datasources provided by this connection.
        /// </summary>
        /// <returns>A <see cref="DataSourceCollection" /> with all the datasources, tables and columns.</returns>
        public async Task<DataSourceCollection> GetDataSources()
            => await apiClient.GetDataSources(apiUrl, cancellationToken);
    }
}