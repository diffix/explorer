namespace Explorer.Explorers.Components
{
    using System.Threading.Tasks;

    internal interface ResultProvider<TResult>
    {
        Task<TResult> ResultAsync { get; }
    }

    internal abstract class ExplorerComponent<TResult> : ResultProvider<TResult>
    {
        private Task<TResult>? componentTask;

        public Task<TResult> ResultAsync
        {
            get => componentTask ??= Task.Run(async () => await Explore());
        }

        protected abstract Task<TResult> Explore();
    }
}