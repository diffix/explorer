namespace Explorer.Api.Models
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.Json.Serialization;
    using Explorer;

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
            ColumnMetrics = exploreParams.Columns
                .Select(s => new
                {
                    Column = s,
                    ColumnType = Diffix.DValueType.Unknown,
                    Status = ExplorationStatus.New,
                    Metrics = Enumerable.Empty<Metrics.ExploreMetric>(),
                })
                .Cast<object>()
                .ToList();
            SampleData = new List<List<object?>>();
        }

        public ExploreResult(Guid explorationId, Exploration exploration)
        {
            Id = explorationId;
            Status = exploration.Status;
            DataSource = exploration.DataSource;
            Table = exploration.Table;
            SampleData = exploration.SampleData.Select(col => col.ToList()).ToList();
            ColumnMetrics = exploration.PublishedMetrics.Select(m => m.Metric).ToList();
        }

        public Guid Id { get; }

        public ExplorationStatus Status { get; }

        public VersionInfo VersionInfo { get; } = VersionInfo.ForThisAssembly();

        public string DataSource { get; }

        public string Table { get; }

        [JsonPropertyName("columns")]
        public List<object> ColumnMetrics { get; }

        public List<List<object?>> SampleData { get; }

        public HashSet<string> Errors { get; } = new HashSet<string>();

        public void AddErrorMessage(string message)
        {
            Errors.Add(message);
        }
    }
}
