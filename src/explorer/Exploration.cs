namespace Explorer
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Lamar;
    using static Explorer.ExplorationStatusEnum;

    public sealed class Exploration : AbstractExploration, IDisposable
    {
        private readonly IContainer rootContainer;
        private readonly ExplorationScopeBuilder scopeBuilder;
        private readonly CancellationTokenSource cancellationTokenSource;
        private bool disposedValue;

        public Exploration(
            IContainer rootContainer,
            ExplorationScopeBuilder scopeBuilder)
        {
            this.rootContainer = rootContainer;
            this.scopeBuilder = scopeBuilder;
            cancellationTokenSource = new CancellationTokenSource();
        }

        public ImmutableArray<ColumnExploration> ColumnExplorations { get; set; }
            = ImmutableArray<ColumnExploration>.Empty;

        public IEnumerable<IEnumerable<object?>> SampleData
        {
            get
            {
                if (!Completion.IsCompletedSuccessfully)
                {
                    yield break;
                }
                var valuesList = ColumnExplorations
                    .Select(ce => ce.PublishedMetrics.SingleOrDefault(m => m.Name == "sample_values")?.Metric as IEnumerable)
                    .Select(metric => metric?.Cast<object?>());
                var numSamples = valuesList.DefaultIfEmpty().Max(col => col?.Count() ?? 0);
                for (var i = 0; i < numSamples; i++)
                {
                    yield return valuesList.Select(sampleColumn => sampleColumn?.ElementAtOrDefault(i));
                }
            }
        }

        public override ExplorationStatus Status { get; protected set; }

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
                            var scope = scopeBuilder.Build(rootContainer.GetNestedContainer(), context);
                            return new ColumnExploration(scope);
                        })
                        .ToImmutableArray();
                });

            // Single-column analyses
            await RunStage(
                ExplorationStatus.Processing,
                async () => await Task.WhenAll(ColumnExplorations.Select(ce => ce.Completion)));

            // Multi-column analyses
            // We have access to all the ColumnExplorations here, so we should be able to extract some
            // context around which column combinations are promising candidates for multi-column analysis.
        }

        private async Task RunStage(ExplorationStatus initialStatus, Func<Task> t)
        {
            Status = initialStatus;
            try
            {
                await t();
            }
            catch
            {
                Status = ExplorationStatus.Error;
                throw;
            }
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
                    cancellationTokenSource.Dispose();
                }
                disposedValue = true;
            }
        }
    }
}
