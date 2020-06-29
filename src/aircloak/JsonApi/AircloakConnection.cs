namespace Aircloak.JsonApi
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    using Diffix;

    /// <summary>
    /// Defines a connection to the Aircloak system that can be used for executing queries.
    /// </summary>
    public class AircloakConnection : DConnection
    {
        private readonly string dataSourceName;
        private readonly Uri apiUrl;
        private readonly JsonApiClient apiClient;
        private readonly TimeSpan pollFrequency;
        private readonly CancellationToken cancellationToken;

        /// <summary>
        /// Initializes a new  instance of the <see cref="AircloakConnection" /> class.
        /// </summary>
        /// <param name="apiClient">The helper API client object which will be used to make HTTP requests to the Aircloak system.</param>
        /// <param name="apiUrl">The base url of the Aircloak Api.</param>
        /// <param name="dataSourceName">The data source to run the queries against.</param>
        /// <param name="pollFrequency">The interval to be used for polling query results.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken" /> that signals the query to abort.</param>
        public AircloakConnection(
            JsonApiClient apiClient,
            Uri apiUrl,
            string dataSourceName,
            TimeSpan pollFrequency,
            CancellationToken cancellationToken)
        {
            this.apiUrl = apiUrl;
            this.apiClient = apiClient;
            this.dataSourceName = dataSourceName;
            this.pollFrequency = pollFrequency;
            this.cancellationToken = cancellationToken;
        }

        /// <inheritdoc />
        public async Task<DResult<TRow>> Exec<TRow>(DQuery<TRow> query)
        {
            return await apiClient.Query(
                apiUrl,
                dataSourceName,
                query,
                pollFrequency,
                cancellationToken);
        }
    }
}