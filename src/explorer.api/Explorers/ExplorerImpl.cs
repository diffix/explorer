namespace Explorer
{
    using System.Collections.Concurrent;
    using System.Threading.Tasks;

    using Aircloak.JsonApi;
    using Aircloak.JsonApi.ResponseTypes;

    internal abstract class ExplorerBase
    {
        private readonly ConcurrentBag<IExploreMetric> metrics;

        private readonly IQueryResolver queryResolver;

        protected ExplorerBase(IQueryResolver queryResolver)
        {
            this.queryResolver = queryResolver;

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
            IQuerySpec<TResult> query,
            System.TimeSpan timeout)
        {
            return await queryResolver.ResolveQuery(query, timeout);
        }
    }
}