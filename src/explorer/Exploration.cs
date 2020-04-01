namespace Explorer
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    using Diffix;
    using Explorer.Common;
    using Explorer.Explorers;

    internal class Exploration : IDisposable
    {
        private readonly IEnumerable<ExplorerBase> explorers;

        private readonly CancellationTokenSource cancellationTokenSource;

        private bool isDisposed;

        public Exploration(IEnumerable<ExplorerBase> explorers)
        {
            this.explorers = explorers;
            isDisposed = false;
            cancellationTokenSource = new CancellationTokenSource();
            ExplorationGuid = Guid.NewGuid();
            Completion = Task.WhenAll(explorers.Select(e => e.Explore(cancellationTokenSource.Token)));
        }

        public Guid ExplorationGuid { get; }

        public IEnumerable<IExploreMetric> ExploreMetrics =>
            explorers.SelectMany(explorer => explorer.Metrics);

        public Task Completion { get; }

        public ExplorationStatus Status =>
            Completion.Status switch
            {
                TaskStatus.Canceled => ExplorationStatus.Canceled,
                TaskStatus.Created => ExplorationStatus.New,
                TaskStatus.Faulted => ExplorationStatus.Error,
                TaskStatus.RanToCompletion => ExplorationStatus.Complete,
                TaskStatus.Running => ExplorationStatus.Processing,
                TaskStatus.WaitingForActivation => ExplorationStatus.Processing,
                TaskStatus.WaitingToRun => ExplorationStatus.Processing,
                TaskStatus.WaitingForChildrenToComplete => ExplorationStatus.Processing,
                _ => throw new System.Exception("Unexpected TaskStatus: '{status}'."),
            };

        public static Exploration? Create(
            IQueryResolver resolver,
            DiffixValueType columnType,
            string tableName,
            string columnName)
        {
            var components = columnType switch
            {
                DiffixValueType.Integer => new ExplorerBase[]
                {
                    new IntegerColumnExplorer(resolver, tableName, columnName, string.Empty),
                    new MinMaxExplorer(resolver, tableName, columnName),
                },
                DiffixValueType.Real => new ExplorerBase[]
                {
                    new RealColumnExplorer(resolver, tableName, columnName),
                    new MinMaxExplorer(resolver, tableName, columnName),
                },
                DiffixValueType.Text => new ExplorerBase[]
                {
                    new TextColumnExplorer(resolver, tableName, columnName),
                    new EmailColumnExplorer(resolver, tableName, columnName),
                    new IntegerColumnExplorer(resolver, tableName, $"length({columnName})", "text.length"),
                },
                DiffixValueType.Bool => new ExplorerBase[]
                {
                    new CategoricalColumnExplorer(resolver, tableName, columnName),
                },
                DiffixValueType.Datetime => new ExplorerBase[]
                {
                    new DatetimeColumnExplorer(resolver, tableName, columnName, columnType),
                },
                DiffixValueType.Timestamp => new ExplorerBase[]
                {
                    new DatetimeColumnExplorer(resolver, tableName, columnName, columnType),
                },
                DiffixValueType.Date => new ExplorerBase[]
                {
                    new DatetimeColumnExplorer(resolver, tableName, columnName, columnType),
                },
                _ => System.Array.Empty<ExplorerBase>(),
            };

            if (components.Length == 0)
            {
                return null;
            }

            return new Exploration(components);
        }

        public void Cancel()
        {
            cancellationTokenSource.Cancel();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!isDisposed)
            {
                if (disposing)
                {
                    cancellationTokenSource.Dispose();
                }
                isDisposed = true;
            }
        }
    }
}