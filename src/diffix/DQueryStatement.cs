namespace Diffix
{
    /// <summary>
    /// Abstract base class for building query statements.
    /// </summary>
    public abstract class DQueryStatement
    {
        /// <summary>
        /// Helper method to build the query statement. This will quote table and column before calling <see cref="GetQueryStatement" />.
        /// </summary>
        /// <param name="table">The table name for which to build the query.</param>
        /// <param name="column">The column name for which to build the query.</param>
        /// <returns>The query string to submit.</returns>
        public string BuildQueryStatement(string table, string column) =>
            GetQueryStatement(Quote(table), Quote(column));

        /// <summary>
        /// Gets the query statement that will generate rows that can be read into instances of <c>TRow</c>.
        /// </summary>
        /// <param name="table">The table name for which to build the query.</param>
        /// <param name="column">The column name for which to build the query.</param>
        /// <returns>The query string to submit.</returns>
        protected abstract string GetQueryStatement(string table, string column);

        private static string Quote(string name) =>
            "\"" + name + "\"";
    }
}