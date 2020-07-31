namespace Explorer.Components
{
    using System.Threading.Tasks;

    public abstract class ExplorerComponent<TResult> : ExplorerComponentBase, ResultProvider<TResult>
    where TResult : class
    {
        private Task<TResult>? componentTask;

        public Task<TResult> ResultAsync => componentTask ??= Task.Run(async () => await Explore());

        protected abstract Task<TResult> Explore();
    }
}