namespace Explorer
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;

    using Aircloak.JsonApi;
    using Explorer.Api.Models;
    using Explorer.Queries;

    internal class RealColumnExplorer : ColumnExplorer
    {
        public RealColumnExplorer(JsonApiClient apiClient, ExploreParams exploreParams)
            : base(apiClient, exploreParams)
        {
        }

        public override async IAsyncEnumerable<ExploreResult> Explore()
        {
            var query = new NumericColumnStats(ExploreParams.TableName, ExploreParams.ColumnName);

            yield return new ExploreResult(ExplorationGuid, status: "waiting");

            var queryResult = await ApiClient.Query<NumericColumnStats.RealResult>(
                ExploreParams.DataSourceName,
                query,
                TimeSpan.FromMinutes(2));

            var stats = queryResult.ResultRows.Single();

            yield return new ExploreResult(ExplorationGuid, status: "complete", metrics: new List<ExploreResult.Metric>
            {
                new ExploreResult.Metric("Min", stats.Min),
                new ExploreResult.Metric("Max", stats.Max),
                new ExploreResult.Metric("Count", stats.Count),
                new ExploreResult.Metric("CountNoise", stats.CountNoise),
            });
        }
    }
}
