namespace Aircloak.JsonApi.Exceptions
{
    using System;
    using System.Runtime.Serialization;

    /// <summary>
    /// Represents errors that occur in Aircloak during query processing.
    /// </summary>
    [Serializable]
    public sealed class QueryException : AircloakException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="QueryException" /> class.
        /// </summary>
        /// <remarks>
        /// One of the default exception constructors.
        /// </remarks>
        [Obsolete("Use the custom constructor instead of the standard excepton constructor.", true)]
        public QueryException()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="QueryException" /> class.
        /// </summary>
        /// <remarks>
        /// One of the default exception constructors.
        /// </remarks>
        /// <param name="message">The exception message.</param>
        [Obsolete("Use the custom constructor instead of the standard excepton constructor.", true)]
        public QueryException(string message)
        : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="QueryException" /> class.
        /// </summary>
        /// <remarks>
        /// One of the default exception constructors.
        /// </remarks>
        /// <param name="message">The exception message.</param>
        /// <param name="innerException">The exception that caused this exception.</param>
        [Obsolete("Use the custom constructor instead of the standard excepton constructor.", true)]
        public QueryException(string message, Exception innerException)
        : base(message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="QueryException" /> class.
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
        private QueryException(SerializationInfo serializationInfo, StreamingContext streamingContext)
        : base(serializationInfo, streamingContext)
        {
        }
    }
}