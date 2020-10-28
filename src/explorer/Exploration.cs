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
    using Microsoft.Extensions.Options;
    using static Explorer.ExplorationStatusEnum;

    public sealed class Exploration : IDisposable
    {
        private readonly IContainer explorationRootContainer;
        private readonly ExplorationScopeBuilder scopeBuilder;
        private readonly CancellationTokenSource cancellationTokenSource;
        private readonly ExplorerOptions options;

        public Exploration(
            IContainer rootContainer,
            ExplorationScopeBuilder scopeBuilder)
        {
            explorationRootContainer = (IContainer)rootContainer.GetNestedContainer();
            this.scopeBuilder = scopeBuilder;
            cancellationTokenSource = new CancellationTokenSource();

            options = explorationRootContainer.GetInstance<IOptions<ExplorerOptions>>().Value;
        }

        public ImmutableArray<ColumnExploration> ColumnExplorations { get; private set; }
            = ImmutableArray<ColumnExploration>.Empty;

        public ExplorationStatus Status { get; private set; }

        public Task Completion => MainTask
            ?? Task.FromException(new InvalidOperationException("Exploration not started."));

        public MultiColumnExploration? MultiColumnExploration { get; private set; }

        public bool MultiColumnEnabled => options.MultiColumnEnabled;

        public int SamplesToPublish => options.SamplesToPublish;

        private Task? MainTask { get; set; }

        public IEnumerable<IEnumerable<object?>> GetSampleData()
        {
            if (Status != ExplorationStatus.Complete)
            {
                yield break;
            }

            var uncorrelatedSampleRows = UncorrelatedSampleRows(SamplesToPublish);

            if (!MultiColumnEnabled)
            {
                foreach (var row in uncorrelatedSampleRows)
                {
                    yield return row;
                }
                yield break;
            }

            var multiColumnMetrics = MultiColumnExploration?.PublishedMetrics
                ?? throw new InvalidOperationException("Expected correlated sample data metric to be available.");

            var metric = multiColumnMetrics
                .SingleOrDefault(m => m.Name == CorrelatedSamples.MetricName)?.Metric;

            if (metric is CorrelatedSamples correlatedSamples)
            {
                foreach (var (row, toInsert) in uncorrelatedSampleRows.Zip(correlatedSamples.ByIndex))
                {
                    // replace uncorrelated with correlated samples where available
                    foreach (var (i, v) in toInsert)
                    {
                        row[i] = v;
                    }

                    yield return row;
                }
            }
            else
            {
                foreach (var row in uncorrelatedSampleRows)
                {
                    yield return row;
                }
            }
        }

        public void CancelExploration()
        {
            cancellationTokenSource.Cancel();
            Status = ExplorationStatus.Canceled;
        }

        public void Dispose()
        {
            foreach (var item in ColumnExplorations)
            {
                item.Dispose();
            }
            explorationRootContainer.Dispose();
            cancellationTokenSource.Dispose();
        }

        public void Explore<TBuilderArgs>(
                ExplorerContextBuilder<TBuilderArgs> contextBuilder,
                TBuilderArgs builderArgs)
        {
            MainTask = Task.Run(async () =>
            {
                // Validation
                await RunStage(
                    ExplorationStatus.Validating,
                    async () =>
                    {
                        var contexts = await contextBuilder.Build(builderArgs, cancellationTokenSource.Token);

                        var singleColumnScopes = contexts
                            .Select(context => scopeBuilder.Build(explorationRootContainer.GetNestedContainer(), context))
                            .ToList();

                        ColumnExplorations = singleColumnScopes
                            .Select(scope => new ColumnExploration(scope))
                            .ToImmutableArray();

                        if (MultiColumnEnabled)
                        {
                            var multiColumnScopeBuilder = new MultiColumnScopeBuilder(
                            singleColumnScopes.Select(_ => _.MetricsPublisher));

                            MultiColumnExploration = new MultiColumnExploration(
                                multiColumnScopeBuilder.Build(
                                    explorationRootContainer.GetNestedContainer(),
                                    contexts.Aggregate((ctx1, ctx2) => ctx1.Merge(ctx2))));
                        }
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
            });
        }

        private List<List<object?>> UncorrelatedSampleRows(int rowCount)
        {
            var uncorrelatedSampleRows = new List<List<object?>>(
                Enumerable.Range(0, rowCount).Select(_ => new List<object?>()));

            foreach (var uncorrelatedSampleColumn in ColumnExplorations
                .Select(ce => (IEnumerable?)ce.PublishedMetrics
                                .SingleOrDefault(m => m.Name == "sample_values")?.Metric)
                .Select(metric => metric?.Cast<object?>() ?? Array.Empty<object?>()))
            {
                for (var i = 0; i < rowCount; i++)
                {
                    uncorrelatedSampleRows[i].Add(uncorrelatedSampleColumn.ElementAtOrDefault(i));
                }
            }

            return uncorrelatedSampleRows;
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
    }
}
