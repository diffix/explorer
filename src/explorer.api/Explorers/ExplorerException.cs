namespace Explorer
{
    [System.Serializable]
    public class ExplorerException : System.Exception
    {
        public ExplorerException()
        {
        }

        public ExplorerException(string message)
        : base(message)
        {
        }

        public ExplorerException(string message, System.Exception inner)
        : base(message, inner)
        {
        }

        protected ExplorerException(
            System.Runtime.Serialization.SerializationInfo info,
            System.Runtime.Serialization.StreamingContext context)
            : base(info, context)
        {
        }
    }
}