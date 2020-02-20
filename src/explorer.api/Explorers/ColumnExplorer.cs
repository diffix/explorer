namespace Explorer
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using Aircloak.JsonApi;
    using Aircloak.JsonApi.ResponseTypes;
    using Explorer.Api.Models;

    internal class ColumnExplorer
    {
        private readonly ConcurrentStack<ExploreResult.Metric> exploreResults;

        private readonly Dictionary<Type, Task> childTasks;

        private readonly Dictionary<Type, ExplorerImpl> childExplorers;

        private readonly ConcurrentDictionary<string, ExploreResult.Metric> exploreMetrics;

        public ColumnExplorer()
        {
            ExplorationGuid = Guid.NewGuid();

            exploreResults = new ConcurrentStack<ExploreResult.Metric>();
            exploreMetrics = new ConcurrentDictionary<string, ExploreResult.Metric>();

            childTasks = new Dictionary<Type, Task>();
            childExplorers = new Dictionary<Type, ExplorerImpl>();
        }

        public Guid ExplorationGuid { get; }

        public IEnumerable<ExploreResult.Metric> ExploreMetrics
        {
            get
            {
                foreach (var explorer in childExplorers.Values)
                {
                    foreach (var metric in explorer.Metrics)
                    {
                        yield return metric;
                    }
                }
            }
        }

        public Task Completion()
        {
            return Task.WhenAll(childTasks.Values);
        }

        public void Spawn(ExplorerImpl explorerImpl)
        {
            var exploreTask = Task.Run(explorerImpl.Explore);
            childExplorers.Add(explorerImpl.GetType(), explorerImpl);
            childTasks.Add(explorerImpl.GetType(), exploreTask);
        }
    }

    internal abstract class ExplorerImpl
    {
        private readonly ConcurrentBag<ExploreResult.Metric> metrics;

        protected ExplorerImpl(JsonApiClient apiClient, ExploreParams exploreParams)
        {
            ApiClient = apiClient;
            ExploreParams = exploreParams;

            metrics = new ConcurrentBag<ExploreResult.Metric>();
        }

        public ExploreResult.Metric[] Metrics
        {
            get => metrics.ToArray();
        }

        public ExploreParams ExploreParams { get; }

        public JsonApiClient ApiClient { get; }

        public abstract Task Explore();

        protected void PublishMetric(ExploreResult.Metric metric)
        {
            metrics.Add(metric);
        }

        protected async Task<QueryResult<TResult>> ResolveQuery<TResult>(
            IQuerySpec<TResult> query,
            TimeSpan timeout)
        {
            return await ApiClient.Query<TResult>(
                ExploreParams.DataSourceName,
                query,
                timeout);
        }
    }
}