namespace Explorer
{
    using System.Collections.Concurrent;
    using System.Threading;
    using System.Threading.Tasks;

    using Aircloak.JsonApi;
    using Aircloak.JsonApi.ResponseTypes;

    internal abstract class ExplorerBase
    {
        private readonly ConcurrentBag<IExploreMetric> metrics;

        private readonly IQueryResolver queryResolver;

        private readonly string metricNamePrefix;

        protected ExplorerBase(IQueryResolver queryResolver, string metricNamePrefix = "")
        {
            this.queryResolver = queryResolver;
            this.metricNamePrefix = metricNamePrefix;
            metrics = new ConcurrentBag<IExploreMetric>();
        }

        public IExploreMetric[] Metrics
        {
            get => metrics.ToArray();
        }

        public abstract Task Explore(CancellationToken cancellationToken);

        protected void PublishMetric(IExploreMetric metric)
        {
            if (!string.IsNullOrEmpty(metricNamePrefix))
            {
                metric.Name = metricNamePrefix + "." + metric.Name;
            }
            metrics.Add(metric);
        }

        protected async Task<QueryResult<TResult>> ResolveQuery<TResult>(
            IQuerySpec<TResult> query,
            CancellationToken cancellationToken)
        {
            return await queryResolver.ResolveQuery(query, cancellationToken);
        }
    }
}