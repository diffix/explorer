namespace Explorer
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;
    using System.Threading.Tasks;

    using Diffix;
    using Explorer.Components;
    using Explorer.Metrics;
    using Explorer.Queries;
    using static Explorer.ExplorationStatusEnum;

    public sealed class MultiColumnExploration : AbstractExploration, IDisposable
    {
        private readonly IEnumerable<ColumnExploration> columnExplorations;
        private bool disposedValue;

        public MultiColumnExploration(IEnumerable<ColumnExploration> columnExplorations)
        {
            Context = columnExplorations.Select(_ => _.Context).Aggregate((ctx1, ctx2) => ctx1.Merge(ctx2));
            this.columnExplorations = columnExplorations;
        }

        public ImmutableArray<string> Columns { get => Context.Columns; }

        public ImmutableArray<DColumnInfo> ColumnInfos { get => Context.ColumnInfos; }

        public ExplorerContext Context { get; }

        public ImmutableArray<ColumnProjection> Projections { get; }

        public List<ExploreMetric> MultiColumnMetrics { get; } = new List<ExploreMetric>();

        public override ExplorationStatus Status { get; protected set; }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        protected async override Task RunTask()
        {
            // Ensure columnExplorations have completed.
            await Task.WhenAll(columnExplorations.Select(_ => _.Completion));

            var projections = columnExplorations
                .Select((c, i) => GetProjection(c, i))
                .Where(p => !(p is IgnoredColumnProjection));

            var correlationComponent = new ColumnCorrelationComponent(projections)
            {
                Context = Context,
            };

            await foreach (var metric in correlationComponent.YieldMetrics())
            {
                MultiColumnMetrics.Add(metric);
            }

            var sampleCorrelator = new CorrelatedSampleGenerator(correlationComponent);

            await foreach (var metric in sampleCorrelator.YieldMetrics())
            {
                MultiColumnMetrics.Add(metric);
            }
        }

        private static ColumnProjection GetProjection(ColumnExploration columnExploration, int index)
        {
            var columnType = columnExploration.Context.ColumnInfo.Type;

            return columnType switch
            {
                DValueType.Date => DateTimeProjection(columnExploration, index),
                DValueType.Datetime => DateTimeProjection(columnExploration, index),
                DValueType.Timestamp => DateTimeProjection(columnExploration, index),
                DValueType.Bool => new IdentityProjection(columnExploration.Column, index, columnType),
                DValueType.Integer => NumericProjection(columnExploration, index),
                DValueType.Real => NumericProjection(columnExploration, index),
                DValueType.Text => TextProjection(columnExploration, index),
                _ => new IdentityProjection(columnExploration.Column, index, columnType),
            };

            static ColumnProjection NumericProjection(ColumnExploration columnExploration, int index)
            {
                var isCategorical = TryGetMetric<bool>(columnExploration, "distinct.is_categorical");

                if (isCategorical)
                {
                    return new IdentityProjection(columnExploration.Column, index, columnExploration.ColumnInfo.Type);
                }

                var distribution = TryGetMetric<NumericDistribution>(columnExploration, "descriptive_stats");

                return new BucketisingProjection(
                    columnExploration.Column, columnExploration.ColumnInfo.Type, index, distribution);
            }

            static ColumnProjection TextProjection(ColumnExploration columnExploration, int index)
            {
                var isCategorical = TryGetMetric<bool>(columnExploration, "distinct.is_categorical");

                if (isCategorical)
                {
                    return new IdentityProjection(columnExploration.Column, index, columnExploration.ColumnInfo.Type);
                }

                // TODO: somehow use string patterns.
                return new IgnoredColumnProjection(columnExploration.Column, index);
            }

            static ColumnProjection DateTimeProjection(ColumnExploration columnExploration, int index)
            {
                var isCategorical = TryGetMetric<bool>(columnExploration, "distinct.is_categorical");

                if (isCategorical)
                {
                    return new IdentityProjection(columnExploration.Column, index, columnExploration.ColumnInfo.Type);
                }

                // TODO: bucketing of time values.
                return new IgnoredColumnProjection(columnExploration.Column, index);
            }
        }

        private static T TryGetMetric<T>(ColumnExploration columnExploration, string metricName)
        {
            try
            {
                var value = columnExploration.PublishedMetrics.Single(metric => metric.Name == metricName).Metric;

                if (value is T typedValue)
                {
                    return typedValue;
                }
                else
                {
                    throw new InvalidOperationException();
                }
            }
            catch (InvalidOperationException)
            {
                throw new InvalidOperationException(
                    $"<{metricName}> metric of type {typeof(T)} was not found but is required for bucket size estimation.");
            }
        }

        private void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // scope.Dispose();
                }
                disposedValue = true;
            }
        }
    }
}