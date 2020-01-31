namespace Explorer
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using Aircloak.JsonApi;
    using Aircloak.JsonApi.ResponseTypes;
    using Explorer.Api.Models;
    using Explorer.Queries;

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
                where TResult : IJsonArrayConvertible, new()
        {
            return await ApiSession.Query<TResult>(
                ExploreParams.DataSourceName,
                query.QueryStatement,
                timeout);
        }
    }
}