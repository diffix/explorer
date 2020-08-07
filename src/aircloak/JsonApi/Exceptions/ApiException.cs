namespace Aircloak.JsonApi.Exceptions
{
    using System;
    using System.Net;
    using System.Net.Http;
    using System.Runtime.Serialization;
    using System.Text.Json;

    /// <summary>
    /// Represents a http error response from the Aircloak Api.
    /// </summary>
    [Serializable]
    public sealed class ApiException : AircloakException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ApiException" /> class.
        /// </summary>
        /// <param name="message">The exception message.</param>
        /// <param name="method">The http method of the request that caused the error.</param>
        /// <param name="endPoint">The api endpoint that the failed request was sent to.</param>
        /// <param name="responseStatus">The http status code returned from the api.</param>
        /// <param name="responseContent">The raw content of the response (expected to be a json-encoded string).
        /// </param>
        public ApiException(
            string message,
            HttpMethod method,
            Uri endPoint,
            HttpStatusCode responseStatus,
            string responseContent)
        : base(message)
        {
            Method = method;
            EndPoint = endPoint;
            ResponseStatus = responseStatus;

            using var jdoc = JsonDocument.Parse(responseContent);
            if (jdoc.RootElement.TryGetProperty("description", out var errorDescription))
            {
                ErrorDescription = errorDescription.GetString();
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ApiException" /> class.
        /// </summary>
        /// <remarks>
        /// One of the default exception constructors. Use the custom constructor above instead.
        /// </remarks>
        [Obsolete("Use the custom constructor instead of the standard excepton constructor.", true)]
        public ApiException()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ApiException" /> class.
        /// </summary>
        /// <remarks>
        /// One of the default exception constructors. Use the custom constructor above instead.
        /// </remarks>
        /// <param name="message">The exception message.</param>
        [Obsolete("Use the custom constructor instead of the standard excepton constructor.", true)]
        public ApiException(string message)
        : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ApiException" /> class.
        /// </summary>
        /// <remarks>
        /// One of the default exception constructors. Use the custom constructor above instead.
        /// </remarks>
        /// <param name="message">The exception message.</param>
        /// <param name="innerException">The exception that caused this exception.</param>
        [Obsolete("Use the custom constructor instead of the standard excepton constructor.", true)]
        public ApiException(string message, Exception innerException)
        : base(message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ApiException" /> class.
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
        private ApiException(SerializationInfo serializationInfo, StreamingContext streamingContext)
        : base(serializationInfo, streamingContext)
        {
        }

        /// <summary>
        /// Gets the <see cref="HttpMethod" /> of the request that caused the error.
        /// </summary>
        public HttpMethod Method { get; } = HttpMethod.Get;

        /// <summary>
        /// Gets the <see cref="Uri" /> of the api endpoint that returned the error.
        /// </summary>
        public Uri EndPoint { get; } = new Uri(string.Empty);

        /// <summary>
        /// Gets the <see cref="HttpStatusCode" /> of the Aircloak Api response.
        /// </summary>
        public HttpStatusCode ResponseStatus { get; }

        /// <summary>
        /// Gets the description of the error returned from the Aircloak Api, if any was provided.
        /// </summary>
        public string? ErrorDescription { get; }
    }
}