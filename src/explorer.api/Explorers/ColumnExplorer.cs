namespace Explorer
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    internal class ColumnExplorer
    {
        private readonly ConcurrentStack<ExploreResult.Metric> exploreResults;

        private readonly Dictionary<Type, Task> childTasks;

        private readonly Dictionary<Type, ExplorerImpl> childExplorers;

        private readonly ConcurrentDictionary<string, ExploreResult.Metric> exploreMetrics;

        public ColumnExplorer()
        {
            ExplorationGuid = Guid.NewGuid();

            exploreResults = new ConcurrentStack<ExploreResult.Metric>();
            exploreMetrics = new ConcurrentDictionary<string, ExploreResult.Metric>();

            childTasks = new Dictionary<Type, Task>();
            childExplorers = new Dictionary<Type, ExplorerImpl>();
        }

        public Guid ExplorationGuid { get; }

        public IEnumerable<ExploreResult.Metric> ExploreMetrics
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