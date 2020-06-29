namespace Diffix
{
    using System.Threading.Tasks;

    /// <summary>
    /// Interface that defines functionality for executing and cancelling queries.
    /// </summary>
    public interface DConnection
    {
        /// <summary>
        /// Executes the query.
        /// </summary>
        /// <param name="query">An object defining the query to be executed.</param>
        /// <typeparam name="TRow">The type of the rows returned by the query.</typeparam>
        /// <returns>An object containing a collection with the rows returned by the query.</returns>
        Task<DResult<TRow>> Exec<TRow>(DQuery<TRow> query);
    }
}
