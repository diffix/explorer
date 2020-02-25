namespace Explorer
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    internal class ColumnExplorer
    {
        private readonly Dictionary<Type, Task> childTasks;

        private readonly Dictionary<Type, ExplorerImpl> childExplorers;

        public ColumnExplorer()
        {
            ExplorationGuid = Guid.NewGuid();

            childTasks = new Dictionary<Type, Task>();
            childExplorers = new Dictionary<Type, ExplorerImpl>();
        }

        public Guid ExplorationGuid { get; }

        public IEnumerable<IExploreMetric> ExploreMetrics
        {
            get
            {
                foreach (var explorer in childExplorers.Values)
                {
                    foreach (var metric in explorer.Metrics)
                    {
                        yield return metric;
                    }
                }
            }
        }

        public Task Completion()
        {
            return Task.WhenAll(childTasks.Values);
        }

        public void Spawn(ExplorerImpl explorerImpl)
        {
            var exploreTask = Task.Run(explorerImpl.Explore);
            childExplorers.Add(explorerImpl.GetType(), explorerImpl);
            childTasks.Add(explorerImpl.GetType(), exploreTask);
        }
    }
}