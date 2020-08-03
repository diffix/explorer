namespace Diffix
{
    using System.Text.Json;

    /// <summary>
    /// Interface for parsing query results.
    /// </summary>
    /// <typeparam name="TRow">A type representing a result row of a query.</typeparam>
    public interface DResultParser<TRow>
    {
        /// <summary>
        /// Parses a row instance.
        /// </summary>
        /// <param name="reader">The <see cref="Utf8JsonReader"/> instance to use for parsing the result.</param>
        /// <returns>The parsed value.</returns>
        public TRow ParseRow(ref Utf8JsonReader reader);
    }
}
