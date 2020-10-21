namespace Explorer
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;
    using System.Threading.Tasks;

    using Diffix;
    using Explorer.Metrics;
    using Explorer.Queries;
    using static Explorer.ExplorationStatusEnum;

    public sealed class MultiColumnExploration : AbstractExploration, IDisposable
    {
        private bool disposedValue;

        public MultiColumnExploration(ExplorationScope scope)
        {
            Scope = scope;
        }

        public ImmutableArray<string> Columns => Context.Columns;

        public ImmutableArray<DColumnInfo> ColumnInfos => Context.ColumnInfos;

        public ExplorerContext Context => Scope.Context;

        public ImmutableArray<ColumnProjection> Projections { get; }

        public List<ExploreMetric> MultiColumnMetrics => Scope.MetricsPublisher.PublishedMetrics.ToList();

        public override ExplorationStatus Status { get; protected set; }

        private ExplorationScope Scope { get; }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        protected async override Task RunTask()
        {
            await Task.WhenAll(Scope.Tasks.Select(async t => await t()));
        }

        private void Dispose(bool disposing)
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