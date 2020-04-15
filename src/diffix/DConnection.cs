namespace Diffix
{
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Interface that defines functionality for executing and cancelling queries.
    /// </summary>
    public interface DConnection
    {
        /// <summary>
        /// Gets a value indicating whether a cancellation was requested for this connection.
        /// </summary>
        bool IsCancellationRequested { get; }

        /// <summary>
        /// Executes the query.
        /// </summary>
        /// <param name="query">An object defining the query to be executed.</param>
        /// <typeparam name="TRow">The type of the rows returned by the query.</typeparam>
        /// <returns>An object containing a collection with the rows returned by the query.</returns>
        Task<DResult<TRow>> Exec<TRow>(DQuery<TRow> query);

        /// <summary>
        /// Requests the cancellation of the still executing queries, started using this connection.
        /// </summary>
        void Cancel();

        /// <summary>
        /// Helper method that throws a <see ref="OperationCanceledException" /> if cancellation was requested using <see cref="Cancel" />.
        /// </summary>
        void ThrowIfCancellationRequested()
        {
            if (IsCancellationRequested)
            {
                throw new OperationCanceledException("Query operation was cancelled.");
            }
        }
    }
}
