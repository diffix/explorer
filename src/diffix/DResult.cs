namespace Diffix
{
    using System.Collections.Generic;

    /// <summary>
    /// Interface which defines the structure for the results returned by a query executed using <see cref="DConnection" />.
    /// </summary>
    /// <typeparam name="TRow">Specifies the type for each of the contained rows.</typeparam>
    public interface DResult<TRow>
    {
        /// <summary>
        /// Gets a value representing a collection of rows returned by the query.
        /// </summary>
        IEnumerable<TRow> Rows { get; }
    }
}