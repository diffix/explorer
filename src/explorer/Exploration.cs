namespace Explorer
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    public sealed class Exploration
    {
        public Exploration(string dataSource, string table, IList<ColumnExploration> columnExplorations)
        {
            DataSource = dataSource;
            Table = table;
            ColumnExplorations = columnExplorations;
            Completion = Task.WhenAll(ColumnExplorations.Select(ce => ce.Completion));
        }

        public string DataSource { get; }

        public string Table { get; }

        public IList<ColumnExploration> ColumnExplorations { get; }

        public Task Completion { get; }

        public ExplorationStatus Status => ConvertToExplorationStatus(Completion.Status);

        public IEnumerable<IEnumerable<object?>> SampleData
        {
            get
            {
                if (!Completion.IsCompletedSuccessfully)
                {
                    yield break;
                }
                var valuesList = ColumnExplorations
                    .Select(ce => ce.PublishedMetrics.SingleOrDefault(m => m.Name == "sample_values")?.Metric as IEnumerable)
                    .Select(metric => metric?.Cast<object?>());
                var numSamples = valuesList.Max(col => col?.Count() ?? 0);
                for (var i = 0; i < numSamples; i++)
                {
                    yield return valuesList.Select(sampleColumn => sampleColumn?.ElementAtOrDefault(i));
                }
            }
        }

        private static ExplorationStatus ConvertToExplorationStatus(TaskStatus status)
        {
            return status switch
            {
                TaskStatus.Canceled => ExplorationStatus.Canceled,
                TaskStatus.Created => ExplorationStatus.New,
                TaskStatus.Faulted => ExplorationStatus.Error,
                TaskStatus.RanToCompletion => ExplorationStatus.Complete,
                TaskStatus.Running => ExplorationStatus.Processing,
                TaskStatus.WaitingForActivation => ExplorationStatus.Processing,
                TaskStatus.WaitingToRun => ExplorationStatus.Processing,
                TaskStatus.WaitingForChildrenToComplete => ExplorationStatus.Processing,
                _ => throw new Exception("Unexpected TaskStatus: '{exploration.Status}'."),
            };
        }
    }
}
