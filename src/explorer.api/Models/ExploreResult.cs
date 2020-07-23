namespace Explorer.Api.Models
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Explorer;

    internal class ExploreResult
    {
        public ExploreResult(Guid explorationId, ExplorationStatus status, string dataSource, string table)
        {
            Id = explorationId;
            Status = status;
            DataSource = dataSource;
            Table = table;
            Columns = new List<object>();
            SampleData = new List<List<object?>>();
        }

        public ExploreResult(Guid explorationId, Exploration exploration)
        {
            Id = explorationId;
            Status = exploration.Status;
            DataSource = exploration.DataSource;
            Table = exploration.Table;
            SampleData = exploration.SampleData.Select(col => col.ToList()).ToList();
            Columns = exploration.PublishedMetrics.Select(m => m.Metric).ToList();
        }

        public Guid Id { get; }

        public ExplorationStatus Status { get; }

        // Note: It would be simpler to define the property as static instead of implementing this through a separate static
        // member variable, however the default json serializer ignores static fields. This is a way to make sure the
        // VersionInfo is included in the serialized output.
#pragma warning disable CA1822 // member can be marked as static
        public VersionInfo VersionInfo { get => VersionInfo.ForThisAssembly(); }
#pragma warning restore CA1822 // member can be marked as static

        public string DataSource { get; }

        public string Table { get; }

        public List<object> Columns { get; }

        public List<List<object?>> SampleData { get; }

        public List<string> Errors { get; } = new List<string>();

        public void AddErrorMessage(string message)
        {
            Errors.Add(message);
        }
    }
}
