namespace Explorer.Api.Models
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.Json.Serialization;

    using Diffix;
    using Explorer;
    using Explorer.Metrics;

    using static ExplorationStatusEnum;

    internal class ExploreResult
    {
        public ExploreResult(
            Guid explorationId,
            ExplorationStatus status,
            ExploreParams exploreParams)
        {
            Id = explorationId;
            Status = status;
            DataSource = exploreParams.DataSource;
            Table = exploreParams.Table;
            ColumnMetrics = exploreParams.Columns.Select(c => new ColumnMetricsType(c)).ToList();
            SampleData = new List<List<object?>>();
            Correlations = new List<ExploreMetric>();
        }

        public ExploreResult(Guid explorationId, Exploration exploration, ExploreParams exploreParams)
        {
            Id = explorationId;
            Status = exploration.Status;
            DataSource = exploreParams.DataSource;
            Table = exploreParams.Table;
            SampleData = exploration.SampleData.Select(col => col.ToList()).ToList();
            ColumnMetrics = exploration.ColumnExplorations.Select(ce => new ColumnMetricsType(ce)).ToList();
            Correlations = exploration.MultiColumnExploration?.PublishedMetrics
                .Where(m => m.Name != CorrelatedSamples.MetricName)
                .ToList()
                ?? new List<ExploreMetric>();
        }

        public Guid Id { get; }

        public ExplorationStatus Status { get; }

        public VersionInfo VersionInfo { get; } = VersionInfo.ForThisAssembly();

        public string DataSource { get; }

        public string Table { get; }

        [JsonPropertyName("columns")]
        public List<ColumnMetricsType> ColumnMetrics { get; }

        public List<List<object?>> SampleData { get; }

        public List<ExploreMetric> Correlations { get; }

        public HashSet<string> Errors { get; } = new HashSet<string>();

        public void AddErrorMessage(string message)
        {
            Errors.Add(message);
        }

        public class ColumnMetricsType
        {
            public ColumnMetricsType(string column)
            {
                Column = column;
                ColumnType = DValueType.Unknown;
                Status = ExplorationStatus.New;
                Metrics = new List<ExploreMetric>(0);
            }

            public ColumnMetricsType(ColumnExploration columnExploration)
            {
                Column = columnExploration.Column;
                ColumnType = columnExploration.ColumnInfo.Type;
                Status = columnExploration.Status;
                Metrics = columnExploration.PublishedMetrics.ToList();
            }

            public string Column { get; }

            public DValueType ColumnType { get; }

            public ExplorationStatus Status { get; }

            public IList<ExploreMetric> Metrics { get; }
        }
    }
}
