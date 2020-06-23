namespace Explorer
{
    using System;
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
