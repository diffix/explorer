namespace Aircloak.JsonApi.Exceptions
{
    [System.Serializable]
    public class QueryException : ApiException
    {
        public QueryException() { }
        public QueryException(string message) : base(message) { }
        public QueryException(string message, System.Exception inner) : base(message, inner) { }
        protected QueryException(
            System.Runtime.Serialization.SerializationInfo info,
            System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}