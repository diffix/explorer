namespace Explorer
{
    using System.Collections.Concurrent;
    using System.Threading.Tasks;

    using Aircloak.JsonApi;
    using Aircloak.JsonApi.ResponseTypes;

    internal abstract class ExplorerImpl
    {
        private readonly ConcurrentBag<ExploreResult.Metric> metrics;

        private readonly IQueryResolver queryResolver;

        protected ExplorerImpl(IQueryResolver queryResolver)
        {
            this.queryResolver = queryResolver;

            metrics = new ConcurrentBag<ExploreResult.Metric>();
        }

        public ExploreResult.Metric[] Metrics
        {
            get => metrics.ToArray();
        }

        public abstract Task Explore();

        protected void PublishMetric(ExploreResult.Metric metric)
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