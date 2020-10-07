namespace Explorer
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Explorer.Metrics;
    using Lamar;
    using static Explorer.ExplorationStatusEnum;

    public sealed class Exploration : AbstractExploration, IDisposable
    {
        private readonly IContainer explorationRootContainer;
        private readonly ExplorationScopeBuilder scopeBuilder;
        private readonly CancellationTokenSource cancellationTokenSource;
        private bool disposedValue;

        public Exploration(
            IContainer rootContainer,
            ExplorationScopeBuilder scopeBuilder)
        {
            explorationRootContainer = (IContainer)rootContainer.GetNestedContainer();
            this.scopeBuilder = scopeBuilder;
            cancellationTokenSource = new CancellationTokenSource();
        }

        public ImmutableArray<ColumnExploration> ColumnExplorations { get; private set; }
            = ImmutableArray<ColumnExploration>.Empty;

        public IEnumerable<IEnumerable<object?>> SampleData
        {
            get
            {
                if (!Completion.IsCompletedSuccessfully)
                {
                    yield break;
                }

                var multiColumnMetrics = MultiColumnExploration?.MultiColumnMetrics
                    ?? throw new InvalidOperationException("Expected correlated sample data metric to be available.");

                var correlatedSamples = (CorrelatedSamples)multiColumnMetrics
                    .SingleOrDefault(m => m.Name == "correlated_samples");

                var uncorrelatedSamples = ColumnExplorations
                    .Select(ce => ce.PublishedMetrics
                                    .SingleOrDefault(m => m.Name == "sample_values")?.Metric as IEnumerable)
                    .Select(metric => metric?.Cast<object?>());

                var r = 0;
                foreach (var indexedCorrelatedSamples in correlatedSamples.ByIndex)
                {
                    // use uncorrelated samples as the default
                    var samples = uncorrelatedSamples.Select(
                            sampleColumn => sampleColumn?.ElementAtOrDefault(r)).ToList();

                    // replace with correlated samples where avilable
                    foreach (var (i, v) in indexedCorrelatedSamples)
                    {
                        samples[i] = v;
                    }

                    yield return samples;
                    r++;
                }
            }
        }

        public object? Correlations => MultiColumnExploration?.MultiColumnMetrics
                                        .SingleOrDefault(m => m.Name == "correlation_factors");

        public override ExplorationStatus Status { get; protected set; }

        public MultiColumnExploration? MultiColumnExploration { get; private set; }

        private Func<Task<IEnumerable<ExplorerContext>>>? ValidationTask { get; set; }

        public void Initialise<TBuilderArgs>(
                ExplorerContextBuilder<TBuilderArgs> contextBuilder,
                TBuilderArgs builderArgs)
            => ValidationTask = async () => await contextBuilder.Build(builderArgs, cancellationTokenSource.Token);

        public void CancelExploration()
        {
            cancellationTokenSource.Cancel();
            Status = ExplorationStatus.Canceled;
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        protected override async Task RunTask()
        {
            // Validation
            await RunStage(
                ExplorationStatus.Validating,
                async () =>
                {
                    var contexts = await (ValidationTask?.Invoke()
                        ?? throw new InvalidOperationException("No validation task specified."));

                    ColumnExplorations = contexts
                        .Select(context =>
                        {
                            var scope = scopeBuilder.Build(explorationRootContainer.GetNestedContainer(), context);
                            return new ColumnExploration(scope);
                        })
                        .ToImmutableArray();

                    MultiColumnExploration = new MultiColumnExploration(ColumnExplorations);
                });

            // Analyses
            await RunStage(
                ExplorationStatus.Processing,
                async () =>
                {
                    await Task.WhenAll(ColumnExplorations.Select(ce => ce.Completion));
                    await (MultiColumnExploration?.Completion ?? Task.CompletedTask);
                });

            // Completed successfully
            Status = ExplorationStatus.Complete;
        }

        private void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    foreach (var item in ColumnExplorations)
                    {
                        item.Dispose();
                    }
                    explorationRootContainer.Dispose();
                    cancellationTokenSource.Dispose();
                }
                disposedValue = true;
            }
        }
    }
}
