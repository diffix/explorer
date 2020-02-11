namespace Explorer
{
    using System;
    using System.Collections.Concurrent;
    using System.Threading.Tasks;

    using Aircloak.JsonApi;
    using Aircloak.JsonApi.ResponseTypes;
    using Explorer.Api.Models;

    internal abstract class ColumnExplorer
    {
        private readonly ConcurrentQueue<ExploreResult> exploreResults;

        protected ColumnExplorer(JsonApiClient apiClient, ExploreParams exploreParams)
        {
            ApiClient = apiClient;
            ExploreParams = exploreParams;
            ExplorationGuid = Guid.NewGuid();

            exploreResults = new ConcurrentQueue<ExploreResult>();
        }

        public Guid ExplorationGuid { get; }

        public ExploreResult LatestResult
        {
            get { return exploreResults.ToArray()[^1]; }
            protected set { exploreResults.Enqueue(value); }
        }

        protected ExploreParams ExploreParams { get; }

        protected JsonApiClient ApiClient { get; }

        public abstract Task Explore();

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