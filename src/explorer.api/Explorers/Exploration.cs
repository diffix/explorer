namespace Explorer
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    internal class Exploration
    {
        private readonly IEnumerable<ExplorerBase> explorers;

        public Exploration(IEnumerable<ExplorerBase> explorers)
        {
            ExplorationGuid = Guid.NewGuid();

            this.explorers = explorers;
            Completion = Task.WhenAll(explorers.Select(async e => await e.Explore()));
        }

        public Guid ExplorationGuid { get; }

        public IEnumerable<IExploreMetric> ExploreMetrics =>
            explorers.SelectMany(explorer => explorer.Metrics);

        public Task Completion { get; }

        public TaskStatus Status => Completion.Status;
    }
}