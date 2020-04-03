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
        private Exploration(
            DConnection conn,
            IEnumerable<(ExplorerBase Explorer, ExplorerContext Context)> components)
        {
            Explorers = components.Select(c => c.Explorer);
            Connection = conn;
            IsDisposed = false;
            ExplorationGuid = Guid.NewGuid();
            Completion = Task.WhenAll(components.Select(c => c.Explorer.Explore(conn, c.Context)));
        }

        public Task Completion { get; }

        public Guid ExplorationGuid { get; }

        public IEnumerable<ExploreMetric> ExploreMetrics =>
            Explorers.SelectMany(explorer => explorer.Metrics);

        public ExplorationStatus Status =>
            ConvertToExplorationStatus(Completion.Status);

        private IEnumerable<ExplorerBase> Explorers { get; }

        private DConnection Connection { get; }

        private bool IsDisposed { get; set; }

        public static Exploration? Create(
            DConnection conn,
            string tableName,
            string columnName,
            DValueType columnType)
        {
            var ctx = new ColumnExplorerContext(tableName, columnName, columnType);
            var components = columnType switch
            {
                DValueType.Integer => new (ExplorerBase, ExplorerContext)[]
                {
                    (new IntegerColumnExplorer(), ctx),
                    (new MinMaxExplorer(), ctx),
                },
                DValueType.Real => new (ExplorerBase, ExplorerContext)[]
                {
                    (new RealColumnExplorer(), ctx),
                    (new MinMaxExplorer(), ctx),
                },
                DValueType.Text => new (ExplorerBase, ExplorerContext)[]
                {
                    (new TextColumnExplorer(), ctx),
                    (new EmailColumnExplorer(), ctx),
                    (new IntegerColumnExplorer("text.length"), new ColumnExplorerContext(tableName, $"length({columnName})", columnType)),
                },
                DValueType.Bool => new (ExplorerBase, ExplorerContext)[]
                {
                    (new CategoricalColumnExplorer(), ctx),
                },
                DValueType.Datetime => new (ExplorerBase, ExplorerContext)[]
                {
                    (new DatetimeColumnExplorer(), ctx),
                },
                DValueType.Timestamp => new (ExplorerBase, ExplorerContext)[]
                {
                    (new DatetimeColumnExplorer(), ctx),
                },
                DValueType.Date => new (ExplorerBase, ExplorerContext)[]
                {
                    (new DatetimeColumnExplorer(), ctx),
                },
                _ => System.Array.Empty<(ExplorerBase, ExplorerContext)>(),
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

        internal static Exploration Create(
            DConnection conn,
            ExplorerBase<ColumnExplorerContext> explorer,
            string tableName,
            string columnName,
            DValueType columnType)
        {
            var ctx = new ColumnExplorerContext(tableName, columnName, columnType);
            var components = new (ExplorerBase, ExplorerContext)[] { (explorer, ctx) };
            return new Exploration(conn, components);
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