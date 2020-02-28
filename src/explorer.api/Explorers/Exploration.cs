namespace Explorer
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    internal class Exploration
    {
        private readonly List<Task> childTasks;

        private readonly List<ExplorerBase> childExplorers;

        public Exploration(IEnumerable<ExplorerBase> explorerImpls)
        {
            ExplorationGuid = Guid.NewGuid();

            childTasks = new List<Task>();
            childExplorers = new List<ExplorerBase>();

            foreach (var impl in explorerImpls)
            {
                Spawn(impl);
            }

            Completion = Task.WhenAll(childTasks);
        }

        public Guid ExplorationGuid { get; }

        public IEnumerable<IExploreMetric> ExploreMetrics =>
            childExplorers.SelectMany(explorer => explorer.Metrics);

        public Task Completion { get; }

        public TaskStatus Status => Completion.Status;

        private void Spawn(ExplorerBase explorerImpl)
        {
            var exploreTask = Task.Run(explorerImpl.Explore);
            childExplorers.Add(explorerImpl);
            childTasks.Add(exploreTask);
        }
    }
}