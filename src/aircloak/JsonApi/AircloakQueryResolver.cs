namespace Aircloak.JsonApi
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    using Diffix;

    public class AircloakQueryResolver : DQueryResolver, IDisposable
    {
        private readonly string dataSourceName;

        private readonly JsonApiClient apiClient;

        private readonly TimeSpan pollFrequency;

        private readonly CancellationTokenSource cancellationTokenSource;

        private bool isDisposed;

        public AircloakQueryResolver(JsonApiClient apiClient, string dataSourceName, TimeSpan pollFrequency)
        {
            this.apiClient = apiClient;
            this.dataSourceName = dataSourceName;
            this.pollFrequency = pollFrequency;
            this.cancellationTokenSource = new CancellationTokenSource();
        }

        public async Task<DResult<TRow>> Resolve<TRow>(DQuery<TRow> query)
        {
            return await apiClient.Query<TRow>(
                dataSourceName,
                query,
                pollFrequency,
                cancellationTokenSource.Token);
        }

        public void Cancel()
        {
            cancellationTokenSource.Cancel();
        }

        public void ThrowIfCancellationRequested()
        {
            cancellationTokenSource.Token.ThrowIfCancellationRequested();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

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