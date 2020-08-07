namespace Explorer
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using Explorer.Metrics;

    using static ExplorationStatusEnum;

    public abstract class AbstractExploration
    {
        private Task? completionTask;

        public ExplorationStatus Status
        {
            // If completionTask is null, that means it has not yet been launched, so status is `New`.
            // Otherwise, derive the ExplorationStatus from the TaskStatus.
            get => completionTask?.Status.ToExplorationStatus() ?? ExplorationStatus.New;
        }

        public abstract IEnumerable<ExploreMetric> PublishedMetrics { get; }

        public Task Completion
        {
            get
            {
                Run();
                return completionTask!;
            }
        }

        public void Run()
        {
            completionTask ??= RunTask();
        }

        public async Task RunAsync()
        {
            Run();
            await completionTask!;
        }

        protected abstract Task RunTask();
    }
}