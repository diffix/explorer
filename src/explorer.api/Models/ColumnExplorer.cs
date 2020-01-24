namespace Explorer
{
    using System;
    using Aircloak.JsonApi;
    using Explorer.Api.Models;

    internal class ColumnExplorer
    {
        internal ColumnExplorer(
            JsonApiSession apiSession,
            ExploreParams exploreParams)
        {
            this.apiSession = apiSession;
            this.exploreParams = exploreParams;

            this.ExplorationGuid = Guid.NewGuid();
        }

        protected readonly ExploreParams exploreParams;

        protected readonly JsonApiSession apiSession;

        internal Guid ExplorationGuid { get; }


    }
}