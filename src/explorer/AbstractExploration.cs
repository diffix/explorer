namespace Explorer
{
    using System.Threading.Tasks;

    using static ExplorationStatusEnum;

    public abstract class AbstractExploration
    {
        private Task? completionTask;

        public abstract ExplorationStatus Status { get; protected set; }

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