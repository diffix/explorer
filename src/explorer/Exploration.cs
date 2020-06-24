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
            Completion = Task.Run(async () =>
            {
                await Task.WhenAll(ColumnExplorations.Select(ce => ce.Completion));
                SampleData = GenerateSampleData();
            });
        }

        public string DataSource { get; }

        public string Table { get; }

        public IList<ColumnExploration> ColumnExplorations { get; }

        public Task Completion { get; }

        public ExplorationStatus Status => ConvertToExplorationStatus(Completion.Status);

        public IEnumerable<IEnumerable<object?>> SampleData { get; private set; } = Array.Empty<IEnumerable<object>>();

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

        private IEnumerable<IEnumerable<object?>> GenerateSampleData()
        {
            var valuesList = ColumnExplorations.Select(ce =>
                ce.PublishedMetrics
                    .Where(m => m.Name == "sample_values")
                    .Select(m => m.Metric as IEnumerable<object?>)
                    .FirstOrDefault());
            var enumerators = valuesList.Select(e => e?.GetEnumerator()).ToArray();
            var sampleData = new List<List<object?>>();
            try
            {
                while (true)
                {
                    var hasData = false;
                    var row = new List<object?>();
                    foreach (var e in enumerators)
                    {
                        if (e?.MoveNext() == true)
                        {
                            hasData = true;
                            row.Add(e.Current);
                        }
                        else
                        {
                            row.Add(null);
                        }
                    }
                    if (!hasData)
                    {
                        break;
                    }
                    sampleData.Add(row);
                }
            }
            finally
            {
                Array.ForEach(enumerators, e => e?.Dispose());
            }
            return sampleData;
        }
    }
}
