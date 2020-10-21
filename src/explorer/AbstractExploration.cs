namespace Explorer
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;
    using System.Threading.Tasks;

    using Diffix;
    using Explorer.Metrics;
    using Microsoft.Extensions.Logging;
    using static Explorer.ExplorationStatusEnum;

    public abstract class AbstractExploration : IDisposable
    {
        private readonly Lazy<Task> completionTask;
        private bool disposedValue;

        protected AbstractExploration(ExplorationScope scope)
        {
            completionTask = new Lazy<Task>(async () => await Explore());
            Scope = scope;
        }

        public ImmutableArray<string> Columns => Context.Columns;

        public ImmutableArray<DColumnInfo> ColumnInfos => Context.ColumnInfos;

        public ExplorerContext Context => Scope.Context;

        public IEnumerable<ExploreMetric> PublishedMetrics => Scope.MetricsPublisher.PublishedMetrics;

        public virtual ExplorationStatus Status { get; protected set; }

        public Task Completion
        {
            get
            {
                return completionTask.Value;
            }
        }

        protected ExplorationScope Scope { get; }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        protected virtual Task Explore() => Task.WhenAll(Scope.Tasks.Select(async explore =>
        {
            try
            {
                Status = ExplorationStatus.Processing;
                await explore();
                Status = ExplorationStatus.Complete;
            }
            catch (Exception ex)
            {
                Status = ExplorationStatus.Error;

                var msg = $"Error in {GetType().Name} for `{Context.DataSource}` / `{Context.Table}` / `{Columns}`.";
                var wrappedEx = new ExplorerException(msg, ex).WithExtraContext(Context);
                Scope.Logger?.LogError(ex, msg, wrappedEx.ExtraContext);

                throw wrappedEx;
            }
        }));

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    Scope.Dispose();
                }
                disposedValue = true;
            }
        }
    }
}