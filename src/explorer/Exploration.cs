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

    public class Exploration : IDisposable
    {
        internal Exploration(DConnection connection, IEnumerable<ExplorerBase> explorers)
        {
            Explorers = explorers;
            Connection = connection;
            IsDisposed = false;
            ExplorationGuid = Guid.NewGuid();
            Completion = Task.WhenAll(explorers.Select(e => e.Explore()));
        }

        public Task Completion { get; }

        public Guid ExplorationGuid { get; }

        public IEnumerable<IExploreMetric> ExploreMetrics =>
            Explorers.SelectMany(explorer => explorer.Metrics);

        public ExplorationStatus Status =>
            ConvertToExplorationStatus(Completion.Status);

        private IEnumerable<ExplorerBase> Explorers { get; }

        private DConnection Connection { get; }

        private bool IsDisposed { get; set; }

        public static Exploration? Create(
            DConnection conn,
            DValueType columnType,
            string tableName,
            string columnName)
        {
            var components = columnType switch
            {
                DValueType.Integer => new ExplorerBase[]
                {
                    new IntegerColumnExplorer(conn, tableName, columnName, string.Empty),
                    new MinMaxExplorer(conn, tableName, columnName),
                },
                DValueType.Real => new ExplorerBase[]
                {
                    new RealColumnExplorer(conn, tableName, columnName),
                    new MinMaxExplorer(conn, tableName, columnName),
                },
                DValueType.Text => new ExplorerBase[]
                {
                    new TextColumnExplorer(conn, tableName, columnName),
                    new EmailColumnExplorer(conn, tableName, columnName),
                    new IntegerColumnExplorer(conn, tableName, $"length({columnName})", "text.length"),
                },
                DValueType.Bool => new ExplorerBase[]
                {
                    new CategoricalColumnExplorer(conn, tableName, columnName),
                },
                DValueType.Datetime => new ExplorerBase[]
                {
                    new DatetimeColumnExplorer(conn, tableName, columnName, columnType),
                },
                DValueType.Timestamp => new ExplorerBase[]
                {
                    new DatetimeColumnExplorer(conn, tableName, columnName, columnType),
                },
                DValueType.Date => new ExplorerBase[]
                {
                    new DatetimeColumnExplorer(conn, tableName, columnName, columnType),
                },
                _ => System.Array.Empty<ExplorerBase>(),
            };

            if (components.Length == 0)
            {
                return null;
            }

            return new Exploration(conn, components);
        }

        public void Cancel()
        {
            Connection.Cancel();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!IsDisposed)
            {
                if (disposing)
                {
                    if (Connection is IDisposable disp)
                    {
                        disp.Dispose();
                    }
                }
                IsDisposed = true;
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
                _ => throw new Exception("Unexpected TaskStatus: '{status}'."),
            };
        }
    }
}