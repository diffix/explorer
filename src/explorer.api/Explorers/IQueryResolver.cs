namespace Explorer
{
    using System.Threading;
    using System.Threading.Tasks;

    using Aircloak.JsonApi;
    using Aircloak.JsonApi.ResponseTypes;

    internal interface IQueryResolver
    {
        public Task<QueryResult<TResult>> ResolveQuery<TResult>(
            IQuerySpec<TResult> query,
            CancellationToken cancellationToken);
    }
}