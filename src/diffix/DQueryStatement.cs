namespace Diffix
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Abstract base class for building query statements.
    /// </summary>
    public abstract class DQueryStatement
    {
        /// <summary>
        /// Helper method to build the query statement. This will quote table and column before calling
        /// <see cref="GetQueryStatement(string, IEnumerable{string})" /> or
        /// <see cref="GetQueryStatement(string, string)" />.
        /// </summary>
        /// <param name="table">The table name for which to build the query.</param>
        /// <param name="columns">The column names for which to build the query.</param>
        /// <returns>The query string to submit.</returns>
        public string BuildQueryStatement(string table, params string[] columns)
        {
            var quoted = columns.Select(Quote);
            return quoted.Count() == 1
                ? GetQueryStatement(Quote(table), quoted.Single())
                : GetQueryStatement(Quote(table), quoted);
        }

        /// <summary>
        /// Gets the query statement that will generate rows that can be read into instances of <c>TRow</c>, for queries
        /// that required a variable number of columns.
        /// </summary>
        /// <remarks>
        /// Classes derived from this class can implement either the single-column or multi-column variant or both.
        /// </remarks>
        /// <param name="table">The table name for which to build the query.</param>
        /// <param name="columns">The column names for which to build the query.</param>
        /// <returns>The query string to submit.</returns>
        protected virtual string GetQueryStatement(string table, IEnumerable<string> columns)
            => throw new InvalidOperationException("This query does not support multi-column execution.");

        /// <summary>
        /// Gets the query statement that will generate rows that can be read into instances of <c>TRow</c>, for queries
        /// that require a single column only.
        /// </summary>
        /// <remarks>
        /// Classes derived from this class can implement either the single-column or multi-column variant or both.
        /// </remarks>
        /// <param name="table">The table name for which to build the query.</param>
        /// <param name="column">The column name for which to build the query.</param>
        /// <returns>The query string to submit.</returns>
        protected virtual string GetQueryStatement(string table, string column)
            => GetQueryStatement(table, new[] { column });

        /// <summary>
        /// Quotes a string.
        /// </summary>
        /// <param name="name">The string to quote.</param>
        /// <returns>The quoted string.</returns>
        private static string Quote(string name) =>
            "\"" + name + "\"";
    }
}