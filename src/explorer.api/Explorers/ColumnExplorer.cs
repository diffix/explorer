namespace Explorer
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using Aircloak.JsonApi;
    using Aircloak.JsonApi.ResponseTypes;
    using Explorer.Api.Models;

    internal abstract class ColumnExplorer
    {
        protected ColumnExplorer(JsonApiClient apiClient, ExploreParams exploreParams)
        {
            ApiClient = apiClient;
            ExploreParams = exploreParams;
            ExplorationGuid = Guid.NewGuid();
        }

        public Guid ExplorationGuid { get; }

        protected ExploreParams ExploreParams { get; }

        protected JsonApiClient ApiClient { get; }

        public abstract IAsyncEnumerable<ExploreResult> Explore();

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