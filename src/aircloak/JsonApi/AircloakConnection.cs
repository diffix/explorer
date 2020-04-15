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

        /// <inheritdoc />
        public bool IsCancellationRequested => cancellationTokenSource.IsCancellationRequested;

        /// <inheritdoc />
        public async Task<DResult<TRow>> Exec<TRow>(DQuery<TRow> query)
        {
            return await apiClient.Query<TRow>(
                dataSourceName,
                query,
                pollFrequency,
                cancellationTokenSource.Token);
        }

        /// <inheritdoc />
        public void Cancel()
        {
            cancellationTokenSource.Cancel();
        }

        /// <inheritdoc />
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