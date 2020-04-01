namespace Diffix
{
    using System.Threading;
    using System.Threading.Tasks;

    public interface IQueryResolver
    {
        public Task<IQueryResult<TResult>> ResolveQuery<TResult>(
            IQuerySpec<TResult> query,
            CancellationToken cancellationToken);
    }
}