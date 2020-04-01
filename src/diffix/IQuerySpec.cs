namespace Diffix
{
    using System.Text.Json;

    /// <summary>
    /// Interface for Aircloak query submission and parsing.
    /// </summary>
    /// <typeparam name="TRow">A type representing a result row of the query.</typeparam>
    public interface IQuerySpec<TRow>
    {
        /// <summary>
        /// Gets the query statement that will generate rows that can be read into instances of <c>TRow</c>.
        /// </summary>
        /// <value>The query string to submit.</value>
        public string QueryStatement { get; }

        /// <summary>
        /// Read the contents of a json array and return an instance of type <c>TRow</c>.
        /// </summary>
        /// <param name="reader">A ref to the <see cref="Utf8JsonReader"/> instance to read from.</param>
        /// <returns>An instance of <c>TRow</c>.</returns>
        public TRow FromJsonArray(ref Utf8JsonReader reader);
    }
}
