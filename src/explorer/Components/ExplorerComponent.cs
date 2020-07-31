namespace Explorer.Components
{
    using System.Threading.Tasks;

    using Lamar;

    public abstract class ExplorerComponent<TResult> : ResultProvider<TResult>
    where TResult : class
    {
        private Task<TResult>? componentTask;

#pragma warning disable CS8618 // Non-nullable property 'Context' is uninitialized. (property is set using Lamar DI)
        [SetterProperty]
        public ExplorerContext Context { get; set; }
#pragma warning restore CS8618

        public Task<TResult> ResultAsync => componentTask ??= Task.Run(async () => await Explore());

        protected abstract Task<TResult> Explore();
    }
}