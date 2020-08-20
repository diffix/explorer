namespace Explorer
{
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    public interface ExplorerContextBuilder<TBuildArgs>
    {
        public Task<IEnumerable<ExplorerContext>> Build(
            TBuildArgs args,
            CancellationToken cancellationToken);
    }
}