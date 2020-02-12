namespace VcrSharp
{
    using System;

    public class PlaybackException : Exception
    {
        public PlaybackException(string message) : base(message)
        {
        }
    }
}
