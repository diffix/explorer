namespace Explorer.Api.Models
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.Json;
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
            SampleData = Array.Empty<IEnumerable<object?>>();
        }

        public ExploreResult(Guid explorationId, Exploration exploration)
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
        }

        public Guid Id { get; }

        public ExplorationStatus Status { get; }

#pragma warning disable CA1822 // member can be marked as static
        // Note:
        // It would be simpler to define the property as static instead of implementing this through a separate static
        // member variable, however the default json serializer ignores static fields. This is a way to make sure the
        // VersionInfo is included in the serialized output.
        public VersionInfo VersionInfo { get => VersionInfo.ForThisAssembly(); }
#pragma warning restore CA1822 // member can be marked as static

        public string DataSource { get; }

        public string Table { get; }

        public IEnumerable<ColumnMetricsCollection> Columns { get; }

        public IEnumerable<IEnumerable<object?>> SampleData { get; }

        public List<string> Errors { get; } = new List<string>();

        public void AddErrorMessage(string message)
        {
            Errors.Add(message);
        }

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
