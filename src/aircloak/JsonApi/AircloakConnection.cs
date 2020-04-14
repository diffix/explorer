namespace Aircloak.JsonApi
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    using Diffix;

    /// <summary>
    /// Defines a connection to the Aircloak system that can be used for executing queries.
    /// </summary>
    public class AircloakConnection : DConnection, IDisposable
    {
        private readonly string dataSourceName;

        private readonly JsonApiClient apiClient;

        private readonly TimeSpan pollFrequency;

        private readonly CancellationTokenSource cancellationTokenSource;

        private bool isDisposed;

        /// <summary>
        /// Initializes a new  instance of the <see cref="AircloakConnection" /> class.
        /// </summary>
        /// <param name="apiClient">The helper API client object which will be used to make HTTP requests to the Aircloak system.</param>
        /// <param name="dataSourceName">The data source to run the queries against.</param>
        /// <param name="pollFrequency">The interval to be used for polling query results.</param>
        public AircloakConnection(JsonApiClient apiClient, string dataSourceName, TimeSpan pollFrequency)
        {
            this.apiClient = apiClient;
            this.dataSourceName = dataSourceName;
            this.pollFrequency = pollFrequency;
            this.cancellationTokenSource = new CancellationTokenSource();
        }

        /// <summary>
        /// Gets a value indicating whether a cancellation was requested for this connection.
        /// </summary>
        public bool IsCancellationRequested => cancellationTokenSource.IsCancellationRequested;

        /// <summary>
        /// Executes the query.
        /// </summary>
        /// <param name="query">An object defining the query to be executed.</param>
        /// <typeparam name="TRow">The type of the rows returned by the query.</typeparam>
        /// <returns>An object containing a collection with the rows returned by the query.</returns>
        public async Task<DResult<TRow>> Exec<TRow>(DQuery<TRow> query)
        {
            return await apiClient.Query<TRow>(
                dataSourceName,
                query,
                pollFrequency,
                cancellationTokenSource.Token);
        }

        /// <summary>
        /// Requests the cancellation of the still executing queries, started using this connection.
        /// </summary>
        public void Cancel()
        {
            cancellationTokenSource.Cancel();
        }

        /// <summary>
        /// Helper method that throws a <see ref="OperationCanceledException" /> if cancellation was requested using <see cref="Cancel" />.
        /// </summary>
        public void ThrowIfCancellationRequested()
        {
            cancellationTokenSource.Token.ThrowIfCancellationRequested();
        }

        /// <summary>
        /// Implements disposing of unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Implements disposing of unmanaged resources.
        /// </summary>
        /// <param name="disposing">Indicates whether the method call comes from a Dispose method
        ///  (its value is true) or from a finalizer (its value is false).
        /// </param>
        protected virtual void Dispose(bool disposing)
        {
            if (!isDisposed)
            {
                if (disposing)
                {
                    cancellationTokenSource.Dispose();
                }
                isDisposed = true;
            }
        }
    }
}