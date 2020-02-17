namespace Explorer
{
    using System;
    using System.Collections.Generic;

    using Aircloak.JsonApi;
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
    }
}