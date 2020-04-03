namespace Explorer.Common
{
    using System.Collections.Concurrent;
    using System.Threading.Tasks;

    using Diffix;

    internal abstract class ExplorerBase
    {
        private readonly ConcurrentBag<IExploreMetric> metrics;

        private readonly DConnection connection;

        private readonly string metricNamePrefix;

        protected ExplorerBase(DConnection connection, string metricNamePrefix = "")
        {
            this.connection = connection;
            this.metricNamePrefix = metricNamePrefix;
            metrics = new ConcurrentBag<IExploreMetric>();
        }

        public IExploreMetric[] Metrics
        {
            get => metrics.ToArray();
        }

        public abstract Task Explore();

        protected void PublishMetric(IExploreMetric metric)
        {
            if (!string.IsNullOrEmpty(metricNamePrefix))
            {
                metric.Name = metricNamePrefix + "." + metric.Name;
            }
            metrics.Add(metric);
        }

        protected async Task<DResult<TRow>> Exec<TRow>(DQuery<TRow> query)
        {
            return await connection.Exec(query);
        }

        protected void ThrowIfCancellationRequested()
        {
            connection.ThrowIfCancellationRequested();
        }
    }
}