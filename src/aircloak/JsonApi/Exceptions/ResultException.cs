namespace Aircloak.JsonApi.Exceptions
{
    using System;
    using System.Runtime.Serialization;

    using Aircloak.JsonApi.ResponseTypes;

    /// <summary>
    /// An exception type for query processing errors.
    /// </summary>
    [Serializable]
    public sealed class ResultException : AircloakException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ResultException" /> class.
        /// </summary>
        /// <remarks>
        /// One of the default exception constructors. Use the FromQueryResult static constructor instead.
        /// </remarks>
        [Obsolete("Use the FromQueryResult static constructor instead.")]
        public ResultException()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ResultException" /> class.
        /// </summary>
        /// <remarks>
        /// One of the default exception constructors. Use the FromQueryResult static constructor instead.
        /// </remarks>
        /// <param name="message">The exception message.</param>
        [Obsolete("Use the FromQueryResult static constructor instead.", true)]
        public ResultException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ResultException" /> class.
        /// </summary>
        /// <remarks>
        /// One of the default exception constructors. Use the FromQueryResult static constructor instead.
        /// </remarks>
        /// <param name="message">The exception message.</param>
        /// <param name="innerException">The exception that caused this exception.</param>
        [Obsolete("Use the FromQueryResult static constructor instead.", true)]
        public ResultException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ResultException" /> class.
        /// </summary>
        /// <param name="queryStatement">The query statement that caused the error.</param>
        /// <param name="queryState">The final query state returned from Aircloak.</param>
        /// <param name="queryError">A description of the error caused by the query.</param>
        private ResultException(string? queryStatement, string? queryState, string? queryError)
        : base($"An error occurred while evaluating the query: {queryError}.")
        {
            QueryStatement = queryStatement;
            QueryState = queryState;
            QueryError = queryError;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ResultException" /> class.
        /// </summary>
        /// <remarks>
        /// This is the constructor that needs to be implemented for classes that are serializable
        /// (see <see cref="SerializableAttribute" />).
        /// </remarks>
        /// <param name="serializationInfo">
        /// The <see cref="SerializationInfo" /> that holds the serialized object data about the exception being thrown.
        /// </param>
        /// <param name="streamingContext">
        /// The <see cref="StreamingContext" /> that contains contextual information about the source or destination.
        /// </param>
        private ResultException(SerializationInfo serializationInfo, StreamingContext streamingContext)
        : base(serializationInfo, streamingContext)
        {
        }

        /// <summary>
        /// Gets the query statement that caused the error, if provided.
        /// </summary>
        public string? QueryStatement { get; }

        /// <summary>
        /// Gets the query statement that caused the error, if provided.
        /// </summary>
        public string? QueryState { get; }

        /// <summary>
        /// Gets the query statement that caused the error, if provided.
        /// </summary>
        public string? QueryError { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ResultException" /> class from a <see cref="QueryResult{T}" />.
        /// </summary>
        /// <param name="queryResult">The <see cref="QueryResult{T}" /> containing the error details.</param>
        /// <typeparam name="TRow">The type of row returned by the query.</typeparam>
        /// <returns>A new instance of the <see cref="ResultException" /> class.</returns>
        internal static ResultException FromQueryResult<TRow>(QueryResult<TRow> queryResult)
            => new ResultException(
                queryResult.Query.Statement,
                queryResult.Query.QueryState,
                queryResult.Query.Error);
    }
}