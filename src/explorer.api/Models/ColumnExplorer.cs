namespace Explorer
{
    using System;
    using Aircloak.JsonApi;
    using Explorer.Api.Models;

    internal class ColumnExplorer
    {
        protected readonly ExploreParams exploreParams;

        protected readonly JsonApiSession apiSession;

        internal ColumnExplorer(
            JsonApiSession apiSession,
            ExploreParams exploreParams)
        {
            this.apiSession = apiSession;
            this.exploreParams = exploreParams;

            ExplorationGuid = Guid.NewGuid();
        }

        internal Guid ExplorationGuid { get; }
    }
}