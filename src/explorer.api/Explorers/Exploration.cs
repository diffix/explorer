namespace Explorer
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    internal class Exploration
    {
        private readonly IEnumerable<ExplorerBase> explorers;

        private readonly CancellationTokenSource cancellationTokenSource;

        public Exploration(IEnumerable<ExplorerBase> explorers, CancellationTokenSource cancellationTokenSource)
        {
            this.explorers = explorers;
            this.cancellationTokenSource = cancellationTokenSource;
            ExplorationGuid = Guid.NewGuid();
            Completion = Task.WhenAll(explorers.Select(async e => await e.Explore()));
        }

        public Guid ExplorationGuid { get; }

        public IEnumerable<IExploreMetric> ExploreMetrics =>
            explorers.SelectMany(explorer => explorer.Metrics);

        public Task Completion { get; }

        public ExploreResult.ExploreStatus Status =>
                Completion.Status switch
                {
                    TaskStatus.Canceled => ExploreResult.ExploreStatus.Canceled,
                    TaskStatus.Created => ExploreResult.ExploreStatus.New,
                    TaskStatus.Faulted => ExploreResult.ExploreStatus.Error,
                    TaskStatus.RanToCompletion => ExploreResult.ExploreStatus.Complete,
                    TaskStatus.Running => ExploreResult.ExploreStatus.Processing,
                    TaskStatus.WaitingForActivation => ExploreResult.ExploreStatus.New,
                    TaskStatus.WaitingToRun => ExploreResult.ExploreStatus.New,
                    TaskStatus.WaitingForChildrenToComplete => ExploreResult.ExploreStatus.Processing,
                    _ => throw new System.Exception("Unexpected TaskStatus: '{status}'."),
                };

        public void Cancel()
        {
            cancellationTokenSource.Cancel();
        }
    }
}