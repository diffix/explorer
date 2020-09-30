namespace Explorer
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Threading.Tasks;

    using Diffix;
    using Explorer.Common;
    using Microsoft.Extensions.Logging;
    using static Explorer.ExplorationStatusEnum;

    public sealed class ColumnExploration : AbstractExploration, IDisposable
    {
        private readonly ExplorationScope scope;
        private bool disposedValue;

        public ColumnExploration(ExplorationScope scope)
        {
            try
            {
                Context = scope.Context;
            }
            catch
            {
                throw new InvalidOperationException(
                    $"{nameof(ColumnExploration)} requires a context object in the {nameof(ExplorationScope)}!");
            }

            try
            {
                MetricsPublisher = scope.MetricsPublisher;
            }
            catch
            {
                throw new InvalidOperationException(
                    $"{nameof(ColumnExploration)} requires a {nameof(MetricsPublisher)} object in the {nameof(ExplorationScope)}!");
            }

            try
            {
                Column = Context.Column;
                ColumnInfo = Context.ColumnInfo;
            }
            catch (InvalidOperationException)
            {
                throw new InvalidOperationException(
                    $"{nameof(ColumnExploration)} requires a single-column context but context has {Context.Columns.Length} columns.");
            }

            this.scope = scope;
        }

        public string Column { get; }

        public DColumnInfo ColumnInfo { get; }

        public ExplorerContext Context { get; }

        public IEnumerable<ExploreMetric> PublishedMetrics => MetricsPublisher.PublishedMetrics;

        public override ExplorationStatus Status { get; protected set; }

        private MetricsPublisher MetricsPublisher { get; }

        public bool TryFindMetric<T>(MetricDefinition<T> metricInfo, [MaybeNull] out T ret)
        where T : notnull
        {
            return MetricsPublisher.TryFindMetric(metricInfo, out ret);
        }

        public T FindMetric<T>(MetricDefinition<T> metricInfo)
        where T : notnull
        {
            return MetricsPublisher.FindMetric(metricInfo);
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        protected override Task RunTask() => Task.WhenAll(scope.Tasks.Select(async t =>
        {
            try
            {
                Status = ExplorationStatus.Processing;
                await t;
                Status = ExplorationStatus.Complete;
            }
            catch (Exception ex)
            {
                Status = ExplorationStatus.Error;

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