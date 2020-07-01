namespace Explorer
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using Explorer.Metrics;
    using Lamar;

    public sealed class ColumnExploration : IDisposable
    {
        private readonly MetricsPublisher publisher;
        private readonly INestedContainer scope;
        private bool disposedValue;

        public ColumnExploration(ExplorationConfig config, INestedContainer scope, string column)
        {
            this.scope = scope;
            publisher = scope.GetInstance<MetricsPublisher>();
            Column = column;
            Completion = Task.WhenAll(config.Tasks);
        }

        public string Column { get; }

        public Task Completion { get; }

        public IEnumerable<ExploreMetric> PublishedMetrics => publisher.PublishedMetrics;

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    scope.Dispose();
                }
                disposedValue = true;
            }
        }
    }
}