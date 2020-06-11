namespace Explorer.Api
{
    [System.Serializable]
    public class MetaDataCheckException : System.Exception
    {
        public MetaDataCheckException()
        {
        }

        public MetaDataCheckException(string message)
        : base(message)
        {
        }

        public MetaDataCheckException(string message, System.Exception inner)
        : base(message, inner)
        {
        }

        protected MetaDataCheckException(
            System.Runtime.Serialization.SerializationInfo info,
            System.Runtime.Serialization.StreamingContext context)
        : base(info, context)
        {
        }
    }
}
