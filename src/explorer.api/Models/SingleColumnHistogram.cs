namespace Explorer
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Numerics;
    using System.Text.Json;
    using System.Threading.Tasks;

    using Aircloak.JsonApi;
    using Aircloak.JsonApi.ResponseTypes;
    using Explorer.Api.Models;

    internal class SingleColumnHistogram : ColumnExplorer
    {
        internal SingleColumnHistogram(JsonApiSession apiSession, ExploreParams exploreParams)
            : base(apiSession, exploreParams)
        {
        }

        internal async IAsyncEnumerable<ExploreResult> Explore()
        {
            var queryParams = new QuerySpec(ExploreParams, new List<decimal>());

            yield return new ExploreResult(ExplorationGuid, status: "waiting");

            var queryResult = await ApiSession.Query<ResultRow>(
                ExploreParams.DataSourceName,
                queryParams.QueryStatement,
                TimeSpan.FromMinutes(2));

            var rows = queryResult.ResultRows;

            yield return new ExploreResult(ExplorationGuid, status: "complete", metrics: new List<ExploreResult.Metric>
            {
                new ExploreResult.Metric("Histogram")
                {
                    MetricType = AircloakType.Real,
                    MetricValue = 0,
                },
            });
        }

        private class QuerySpec : IQuerySpec<ResultRow>
        {
            public QuerySpec(ExploreParams exploreParams, List<decimal> buckets)
            {
                TableName = exploreParams.TableName;
                ColumnName = exploreParams.ColumnName;
                Buckets = buckets;
            }

            public string QueryStatement
            {
                get
                {
                    var bucketsFragment = string.Join(
                        ",",
                        from bucket in Buckets select $"bucket({ColumnName} by {bucket})");

                    return $@"
                        select
                            grouping_id(
                                {bucketsFragment}
                            ),
                            {Buckets.Count}
                            {bucketsFragment},
                            count(*),
                            count_noise(*)
                        from {TableName}
                        group by grouping sets ({Enumerable.Range(1, Buckets.Count)})";
                }
            }

            public string TableName { get; }

            public string ColumnName { get; }

            public List<decimal> Buckets { get; }
        }

        private class ResultRow : IJsonArrayConvertible
        {
#pragma warning disable RCS1074 // Remove redundant constructor
            // Need this for JsonArrayConverter constraint
            public ResultRow()
            {
            }
#pragma warning restore RCS1074 // Remove redundant constructor

            internal double? BucketSize { get; set; }

            internal double? LowerBound { get; set; }

            internal int? Count { get; set; }

            internal double? CountNoise { get; set; }

            void IJsonArrayConvertible.FromArrayValues(ref Utf8JsonReader reader)
            {
                reader.Read();
                var groupingFlags = reader.GetInt32();
                reader.Read();
                var numBuckets = reader.GetInt32();

                for (var i = 0; i < numBuckets; i++)
                {
                    reader.Read();
                    if ((groupingFlags & 1) == 0)
                    {
                        LowerBound = reader.GetDouble();
                    }
                }

                reader.Read();
                Count = reader.GetInt32();
                reader.Read();
                CountNoise = reader.GetInt32();
            }
        }
    }
}