namespace Explorer.Api.Models
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    using Explorer;

    internal class ExploreResult
    {
        public ExploreResult(Guid explorationId, ExplorationStatus status, string dataSource, string table)
        {
            Id = explorationId;
            Status = status;
            DataSource = dataSource;
            Table = table;
            Columns = Array.Empty<ColumnMetricsCollection>();
        }

        public ExploreResult(Guid explorationId, ExplorationStatus status, string dataSource, string table, IEnumerable<ColumnMetricsCollection> columnMetrics)
        {
            Id = explorationId;
            Status = status;
            DataSource = dataSource;
            Table = table;
            Columns = columnMetrics;
        }

        public Guid Id { get; }

        public ExplorationStatus Status { get; }

        public string DataSource { get; }

        public string Table { get; }

        public IEnumerable<ColumnMetricsCollection> Columns { get; }

        public class ColumnMetricsCollection
        {
            public ColumnMetricsCollection(string column, IEnumerable<Metric> metrics)
            {
                Column = column;
                Metrics = metrics;
            }

            public string Column { get; }

            public IEnumerable<Metric> Metrics { get; }
        }

        public class Metric
        {
            public Metric(string name, object value)
            {
                MetricName = name;
                MetricValue = value;
            }

            [JsonPropertyName("name")]
            public string MetricName { get; set; }

            [JsonPropertyName("value")]
            public object MetricValue { get; set; }
        }
    }
}
