namespace Aircloak.JsonApi.Exceptions
{
    [System.Serializable]
    public class ResultException : ApiException
    {
        public ResultException() { }
        public ResultException(string message) : base(message) { }
        public ResultException(string message, System.Exception inner) : base(message, inner) { }
        protected ResultException(
            System.Runtime.Serialization.SerializationInfo info,
            System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}