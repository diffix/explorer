namespace Diffix
{
    using System.Text.Json;
    using System.Threading.Tasks;

    /// <summary>
    /// Parses a row instance.
    /// </summary>
    /// <param name="reader">The <see cref="Utf8JsonReader"/> instance to use for parsing the result.</param>
    /// <typeparam name="TRow">The type of the rows returned by the query.</typeparam>
    /// <returns>The parsed value.</returns>
    public delegate TRow DRowParser<TRow>(ref Utf8JsonReader reader);

    /// <summary>
    /// Interface that defines functionality for executing and cancelling queries.
    /// </summary>
    public interface DConnection
    {
        /// <summary>
        /// Executes the query.
        /// </summary>
        /// <param name="query">An object defining the query to be executed.</param>
        /// <param name="rowParser">A delegate used for parsing a result row.</param>
        /// <typeparam name="TRow">The type of the rows returned by the query.</typeparam>
        /// <returns>An object containing a collection with the rows returned by the query.</returns>
        Task<DResult<TRow>> Exec<TRow>(string query, DRowParser<TRow> rowParser);
    }
}
