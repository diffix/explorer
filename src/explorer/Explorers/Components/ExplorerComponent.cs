namespace Explorer.Explorers.Components
{
    using System.Threading.Tasks;

    using Diffix;
    using Explorer.Common;

    internal abstract class ExplorerComponent<TResult>
    {
        private Task<TResult>? componentTask;

        protected ExplorerComponent(DConnection conn, ExplorerContext ctx)
        {
            Conn = conn;
            Ctx = ctx;
        }

        public DConnection Conn { get; }

        public ExplorerContext Ctx { get; }

        public Task<TResult> ResultAsync
        {
            get => componentTask ??= Task.Run(async () => await Explore());
        }

        public ExplorerComponent<TResult> LinkToDependentComponent(DependsOn<TResult> component)
        {
            component.LinkToSourceComponent(this);
            return this;
        }

        protected abstract Task<TResult> Explore();
    }
}