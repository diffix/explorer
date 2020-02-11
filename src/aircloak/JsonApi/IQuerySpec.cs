namespace Aircloak.JsonApi
{
    /// <summary>
    /// Interface for Aircloak query submission and parsing.
    /// </summary>
    /// <typeparam name="TRow">A type representing a result row of the query.</typeparam>
    /// <remarks>
    /// Implementations of this interface must also implement <see cref="IRowReader{TRow}"/>.
    /// </remarks>
    public interface IQuerySpec<TRow> : IRowReader<TRow>
    {
        /// <summary>
        /// Gets the query statement that will generate rows that can be read into instances of <c>TRow</c>.
        /// </summary>
        /// <value>The query string to submit.</value>
        public string QueryStatement { get; }
    }
}
