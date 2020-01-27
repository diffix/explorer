namespace Explorer
{
    using System;
    using Aircloak.JsonApi;
    using Explorer.Api.Models;

    internal class ColumnExplorer
    {
        public ColumnExplorer(
            JsonApiSession apiSession,
            ExploreParams exploreParams)
        {
            ApiSession = apiSession;
            ExploreParams = exploreParams;

            ExplorationGuid = Guid.NewGuid();
        }

        public Guid ExplorationGuid { get; }

        protected ExploreParams ExploreParams { get; }

        protected JsonApiSession ApiSession { get; }
    }
}