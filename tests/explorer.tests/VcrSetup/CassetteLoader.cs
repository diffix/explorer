namespace Explorer.Tests
{
    using System;
    using System.Collections.Generic;
    using VcrSharp;

    public class CassetteLoader : IDisposable
    {
        private readonly Dictionary<string, Cassette> cassettes =
            new Dictionary<string, Cassette>();
        private bool disposedValue;

        public Cassette LoadCassette(string path)
        {
            if (!cassettes.ContainsKey(path))
            {
                cassettes[path] = new Cassette(path);
            }

            return cassettes[path];
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    foreach (var cassette in cassettes.Values)
                    {
                        cassette.Dispose();
                    }
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
