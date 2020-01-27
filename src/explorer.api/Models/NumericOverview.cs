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

        internal async Task<ExploreResult> Explore()
        {
            var queryParams = new QuerySpec(exploreParams);

            var queryResult = await apiSession.Query<ResultRow>(
                exploreParams.DataSourceName,
                queryParams.QueryStatement,
                TimeSpan.FromMinutes(2))
                .ConfigureAwait(false);

            var rows = queryResult.ResultRows;
            Debug.Assert(
                rows.Count() == 1,
                $"Expected query {queryParams.QueryStatement} to return exactly one row.");

            var stats = rows.First();

            return new ExploreResult
            {
                Status = "complete",
                Id = ExplorationGuid,
                Metrics = new List<ExploreResult.Metric>
                {
                    {
                        new ExploreResult.Metric
                        {
                            MetricName = "Min",
                            MetricType = AircloakType.Real,
                            MetricValue = stats.Min,
                        }
                    },
                    {
                        new ExploreResult.Metric
                        {
                            MetricName = "Max",
                            MetricType = AircloakType.Real,
                            MetricValue = stats.Max,
                        }
                    },
                    {
                        new ExploreResult.Metric
                        {
                            MetricName = "Count",
                            MetricType = AircloakType.Integer,
                            MetricValue = stats.Count,
                        }
                    },
                    {
                        new ExploreResult.Metric
                        {
                            MetricName = "CountNoise",
                            MetricType = AircloakType.Real,
                            MetricValue = stats.CountNoise,
                        }
                    },
                },
            };
        }

        internal class QuerySpec : IQuerySpec<ResultRow>
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

        internal class ResultRow : IJsonArrayConvertible
        {
#pragma warning disable RCS1074 // Remove redundant constructor
            // Need this for JsonArrayConverter constraint
            public ResultRow()
            {
            }
#pragma warning restore RCS1074 // Remove redundant constructor

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

            void IJsonArrayConvertible.ToArrayValues(Utf8JsonWriter writer)
            {
                WriteNullableNumberValue(writer, Min);
                WriteNullableNumberValue(writer, Max);
                WriteNullableNumberValue(writer, Count);
                WriteNullableNumberValue(writer, CountNoise);
            }

            private void WriteNullableNumberValue<T>(Utf8JsonWriter writer, T? value)
                where T : unmanaged
            {
                if (value.HasValue)
                {
                    dynamic number = value.Value;
                    writer.WriteNumberValue(number);
                }
                else
                {
                    writer.WriteNullValue();
                }
            }
        }
    }
}
