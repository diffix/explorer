namespace Aircloak.JsonApi.Exceptions
{
    using System.Runtime.Serialization;

    /// <summary>
    /// Represents errors that occur during queries processing.
    /// </summary>
    [System.Serializable]
    public class QueryException : ApiException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="QueryException" /> class.
        /// </summary>
        public QueryException()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="QueryException" /> class with a specified error message.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        public QueryException(string message)
        : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="QueryException" /> class with a specified error message and a reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="inner">The exception that is the cause of the current exception, or a null reference (Nothing in Visual Basic) if no inner exception is specified.</param>
        public QueryException(string message, System.Exception inner)
        : base(message, inner)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="QueryException" /> class with serialized data.
        /// </summary>
        /// <param name="info">The <see cref="SerializationInfo" /> that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">The <see cref="StreamingContext" /> that contains contextual information about the source or destination.</param>
        protected QueryException(SerializationInfo info, StreamingContext context)
        : base(info, context)
        {
        }
    }
}