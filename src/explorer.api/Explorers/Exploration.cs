namespace Explorer
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    internal class Exploration
    {
        private readonly List<Task> childTasks;

        private readonly List<ExplorerBase> childExplorers;

        private readonly CancellationTokenSource cancellationTokenSource;

        public Exploration(IEnumerable<ExplorerBase> explorers, CancellationTokenSource cts)
        {
            ExplorationGuid = Guid.NewGuid();
            childTasks = new List<Task>();
            childExplorers = new List<ExplorerBase>();
            cancellationTokenSource = cts;

            foreach (var explorer in explorers)
            {
                Spawn(explorer);
            }

            Completion = Task.WhenAll(childTasks);
        }

        public Guid ExplorationGuid { get; }

        public IEnumerable<IExploreMetric> ExploreMetrics =>
            childExplorers.SelectMany(explorer => explorer.Metrics);

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

        private void Spawn(ExplorerBase explorer)
        {
            var exploreTask = Task.Run(explorer.Explore, cancellationTokenSource.Token);
            childExplorers.Add(explorer);
            childTasks.Add(exploreTask);
        }
    }
}