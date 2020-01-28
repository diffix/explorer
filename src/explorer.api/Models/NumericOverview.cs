namespace Explorer
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Text.Json;
    using System.Threading.Tasks;

    using Aircloak.JsonApi;
    using Aircloak.JsonApi.ResponseTypes;
    using Explorer.Api.Models;

    internal class NumericOverview : ColumnExplorer
    {
        internal NumericOverview(JsonApiSession apiSession, ExploreParams exploreParams)
            : base(apiSession, exploreParams)
        {
        }

        internal async IAsyncEnumerable<ExploreResult> Explore()
        {
            var queryParams = new QuerySpec(ExploreParams);

            yield return new ExploreResult(ExplorationGuid, status: "waiting");

            var queryResult = await ApiSession.Query<ResultRow>(
                ExploreParams.DataSourceName,
                queryParams.QueryStatement,
                TimeSpan.FromMinutes(2));

            var rows = queryResult.ResultRows;
            Debug.Assert(
                rows.Count() == 1,
                $"Expected query {queryParams.QueryStatement} to return exactly one row.");

            var stats = rows.First();

            yield return new ExploreResult(ExplorationGuid, status: "complete", metrics: new List<ExploreResult.Metric>
            {
                new ExploreResult.Metric("Min")
                {
                    MetricType = AircloakType.Real,
                    MetricValue = stats.Min,
                },
                new ExploreResult.Metric("Max")
                {
                    MetricType = AircloakType.Real,
                    MetricValue = stats.Max,
                },
                new ExploreResult.Metric("Count")
                {
                    MetricType = AircloakType.Integer,
                    MetricValue = stats.Count,
                },
                new ExploreResult.Metric("CountNoise")
                {
                    MetricType = AircloakType.Real,
                    MetricValue = stats.CountNoise,
                },
            });
        }

        private class QuerySpec : IQuerySpec<ResultRow>
        {
            public QuerySpec(ExploreParams exploreParams)
            {
                TableName = exploreParams.TableName;
                ColumnName = exploreParams.ColumnName;
            }

            public string QueryStatement => $@"
                        select
                            min({ColumnName}),
                            max({ColumnName}),
                            count(*),
                            count_noise(*)
                        from {TableName}";

            public string TableName { get; }

            public string ColumnName { get; }
        }

        private class ResultRow : IJsonArrayConvertible
        {
            internal double? Min { get; set; }

            internal double? Max { get; set; }

            internal int? Count { get; set; }

            internal double? CountNoise { get; set; }

            void IJsonArrayConvertible.FromArrayValues(ref Utf8JsonReader reader)
            {
                reader.Read();
                Min = reader.GetDouble();
                reader.Read();
                Max = reader.GetDouble();
                reader.Read();
                Count = reader.GetInt32();
                reader.Read();
                CountNoise = reader.GetDouble();
            }
        }
    }
}
