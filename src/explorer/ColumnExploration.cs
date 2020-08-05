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
            if (scope.Ctx is null)
            {
                throw new InvalidOperationException(
                    $"Can't build {GetType().Name} without a context object in scope!");
            }

            this.scope = scope;
        }

        public string Column { get => Ctx.Column; }

        public DColumnInfo ColumnInfo { get => Ctx.ColumnInfo; }

        public ExplorerContext Ctx { get => scope.Ctx!; }

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
                var msg = $"Error in column exploration for `{Ctx.DataSource}` / `{Ctx.Table}` / `{Column}`.";
                var wrappedEx = new ExplorerException(msg, ex).WithExtraContext(Ctx);
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