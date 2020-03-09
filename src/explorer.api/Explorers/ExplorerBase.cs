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

        private readonly CancellationToken cancellationToken;

        protected ExplorerBase(IQueryResolver queryResolver, CancellationToken cancellationToken)
        {
            this.queryResolver = queryResolver;
            this.cancellationToken = cancellationToken;
            metrics = new ConcurrentBag<IExploreMetric>();
        }

        public IExploreMetric[] Metrics
        {
            get => metrics.ToArray();
        }

        public abstract Task Explore();

        protected void PublishMetric(IExploreMetric metric)
        {
            metrics.Add(metric);
        }

        protected async Task<QueryResult<TResult>> ResolveQuery<TResult>(
            IQuerySpec<TResult> query)
        {
            return await queryResolver.ResolveQuery(query, cancellationToken);
        }
    }
}