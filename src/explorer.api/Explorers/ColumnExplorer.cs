namespace Explorer
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Concurrent;
    using System.Threading.Tasks;

    using Aircloak.JsonApi;
    using Aircloak.JsonApi.ResponseTypes;
    using Explorer.Api.Models;

    internal abstract class ColumnExplorer
    {
        private readonly ConcurrentStack<ExploreResult> exploreResults;

        protected ColumnExplorer(JsonApiClient apiClient, ExploreParams exploreParams)
        {
            ApiClient = apiClient;
            ExploreParams = exploreParams;
            ExplorationGuid = Guid.NewGuid();

            exploreResults = new ConcurrentStack<ExploreResult>();
            exploreResults.Push(new ExploreResult(ExplorationGuid, Status.New));
        }

        public Guid ExplorationGuid { get; }

        public ExploreResult LatestResult
        {
            get
            {
                if (exploreResults.TryPeek(out var result))
                {
                    return result;
                }
                else
                {
                    return new ExploreError(ExplorationGuid, "Unexpected error: unknown explorer status.");
                }
            }

            protected set
            {
                exploreResults.Push(value);
            }
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

        public static class Status
        {
            public const string New = "new";
            public const string Processing = "processing";
            public const string Error = "error";
            public const string Complete = "complete";
        }
    }
}