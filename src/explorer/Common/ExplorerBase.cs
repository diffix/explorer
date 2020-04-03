namespace Explorer.Common
{
    using System.Collections.Concurrent;
    using System.Threading.Tasks;

    using Diffix;

    internal abstract class ExplorerBase
    {
        private readonly ConcurrentBag<IExploreMetric> metrics;

        private readonly DQueryResolver queryResolver;

        private readonly string metricNamePrefix;

        protected ExplorerBase(DQueryResolver queryResolver, string metricNamePrefix = "")
        {
            this.queryResolver = queryResolver;
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

        protected async Task<DResult<TRow>> ResolveQuery<TRow>(DQuery<TRow> query)
        {
            return await queryResolver.Resolve(query);
        }

        protected void ThrowIfCancellationRequested()
        {
            queryResolver.ThrowIfCancellationRequested();
        }
    }
}