namespace Explorer
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Aircloak.JsonApi.ResponseTypes;

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

        public ExploreResult.ExploreStatus Status =>
            Completion.Status switch
            {
                TaskStatus.Canceled => ExploreResult.ExploreStatus.Canceled,
                TaskStatus.Created => ExploreResult.ExploreStatus.New,
                TaskStatus.Faulted => ExploreResult.ExploreStatus.Error,
                TaskStatus.RanToCompletion => ExploreResult.ExploreStatus.Complete,
                TaskStatus.Running => ExploreResult.ExploreStatus.Processing,
                TaskStatus.WaitingForActivation => ExploreResult.ExploreStatus.Processing,
                TaskStatus.WaitingToRun => ExploreResult.ExploreStatus.Processing,
                TaskStatus.WaitingForChildrenToComplete => ExploreResult.ExploreStatus.Processing,
                _ => throw new System.Exception("Unexpected TaskStatus: '{status}'."),
            };

        public static Exploration? Create(
            AircloakQueryResolver resolver,
            AircloakType columnType,
            string tableName,
            string columnName)
        {
            var components = columnType switch
            {
                AircloakType.Integer => new ExplorerBase[]
                {
                    new IntegerColumnExplorer(resolver, tableName, columnName, string.Empty),
                    new MinMaxExplorer(resolver, tableName, columnName),
                },
                AircloakType.Real => new ExplorerBase[]
                {
                    new RealColumnExplorer(resolver, tableName, columnName),
                    new MinMaxExplorer(resolver, tableName, columnName),
                },
                AircloakType.Text => new ExplorerBase[]
                {
                    new TextColumnExplorer(resolver, tableName, columnName),
                    new EmailColumnExplorer(resolver, tableName, columnName),
                    new IntegerColumnExplorer(resolver, tableName, $"length({columnName})", "text.length"),
                },
                AircloakType.Bool => new ExplorerBase[]
                {
                    new CategoricalColumnExplorer(resolver, tableName, columnName),
                },
                AircloakType.Datetime => new ExplorerBase[]
                {
                    new DatetimeColumnExplorer(resolver, tableName, columnName, columnType),
                },
                AircloakType.Timestamp => new ExplorerBase[]
                {
                    new DatetimeColumnExplorer(resolver, tableName, columnName, columnType),
                },
                AircloakType.Date => new ExplorerBase[]
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