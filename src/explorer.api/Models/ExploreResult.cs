namespace Explorer.Api.Models
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.Json.Serialization;

    using Explorer;

    internal class ExploreResult
    {
        public ExploreResult(Guid explorationId, ExplorationStatus status, string dataSource, string table, VersionInfo versionInfo)
        {
            Id = explorationId;
            Status = status;
            DataSource = dataSource;
            Table = table;
            Columns = Array.Empty<ColumnMetricsCollection>();
            SampleData = Array.Empty<IEnumerable<object?>>();
            VersionInfo = versionInfo;
        }

        public ExploreResult(Guid explorationId, Exploration exploration, VersionInfo versionInfo)
        {
            Id = explorationId;
            Status = exploration.Status;
            DataSource = exploration.DataSource;
            Table = exploration.Table;
            SampleData = exploration.SampleData;
            Columns = exploration.ColumnExplorations.Select(ce =>
                new ColumnMetricsCollection(
                    ce.Column,
                    ce.PublishedMetrics.Select(m => new Metric(m.Name, m.Metric))));
            VersionInfo = versionInfo;
        }

        public Guid Id { get; }

        public ExplorationStatus Status { get; }

        public VersionInfo VersionInfo { get; }

        public string DataSource { get; }

        public string Table { get; }

        public IEnumerable<ColumnMetricsCollection> Columns { get; }

        public IEnumerable<IEnumerable<object?>> SampleData { get; }

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
