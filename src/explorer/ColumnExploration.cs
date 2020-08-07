namespace Explorer
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using Diffix;
    using Explorer.Metrics;
    using Microsoft.Extensions.Logging;

    public sealed class ColumnExploration : AbstractExploration, IDisposable
    {
        private readonly ExplorationScope scope;
        private bool disposedValue;

        public ColumnExploration(ExplorationScope scope)
        {
            if (scope.Context is null)
            {
                throw new InvalidOperationException(
                    $"Can't build {GetType().Name} without a context object in scope!");
            }

            this.scope = scope;
        }

        public string Column { get => Context.Column; }

        public DColumnInfo ColumnInfo { get => Context.ColumnInfo; }

        public ExplorerContext Context { get => scope.Context!; }

        public override IEnumerable<ExploreMetric> PublishedMetrics =>
            scope.MetricsPublisher.PublishedMetrics;

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        protected override Task RunTask() => Task.WhenAll(scope.Tasks.Select(async t =>
        {
            try
            {
                await t;
            }
            catch (Exception ex)
            {
                var msg = $"Error in column exploration for `{Context.DataSource}` / `{Context.Table}` / `{Column}`.";
                var wrappedEx = new ExplorerException(msg, ex).WithExtraContext(Context);
                scope.Logger.LogError(ex, msg, wrappedEx.ExtraContext);

                throw wrappedEx;
            }
        }));

        private void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    scope.Dispose();
                }
                disposedValue = true;
            }
        }
    }
}