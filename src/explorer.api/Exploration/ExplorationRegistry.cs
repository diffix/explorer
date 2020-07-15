namespace Explorer.Api
{
    using System;
    using System.Collections.Concurrent;
    using System.Threading;
    using Explorer;
    using Microsoft.Extensions.Logging;

    public class ExplorationRegistry
    {
        private readonly ConcurrentDictionary<Guid, Registration> registrations =
            new ConcurrentDictionary<Guid, Registration>();

        public ExplorationRegistry(ILogger<ExplorationRegistry> logger)
        {
            Logger = logger;
        }

        public ILogger<ExplorationRegistry> Logger { get; }

        public Guid Register(Exploration exploration, CancellationTokenSource tokenSource)
        {
            var id = Guid.NewGuid();
            registrations[id] = new Registration(exploration, tokenSource);

            return id;
        }

        public void Remove(Guid id)
        {
            if (!registrations.TryRemove(id, out var registration))
            {
                return;
            }

            if (registration.Status == ExplorationStatus.Processing || registration.Status == ExplorationStatus.New)
            {
                throw new InvalidOperationException("Exploration should not be removed before completion.");
            }

            registration.Dispose();
        }

        public ExplorationStatus GetStatus(Guid id) => registrations[id].Status;

        public Exploration GetExploration(Guid id) => registrations[id].Exploration;

        public void CancelExploration(Guid id)
        {
            registrations[id].CancelExploration();
        }

        private class Registration : IDisposable
        {
            private readonly CancellationTokenSource tokenSource;

            private bool disposedValue;

            public Registration(Exploration exploration, CancellationTokenSource tokenSource)
            {
                Exploration = exploration;
                this.tokenSource = tokenSource;
            }

            public Exploration Exploration { get; }

            public ExplorationStatus Status
            {
                get
                {
                    if (tokenSource.IsCancellationRequested)
                    {
                        return ExplorationStatus.Canceled;
                    }
                    return Exploration.Status;
                }
            }

            public void CancelExploration() => tokenSource.Cancel();

            public void Dispose()
            {
                // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
                Dispose(disposing: true);
                GC.SuppressFinalize(this);
            }

            protected virtual void Dispose(bool disposing)
            {
                if (!disposedValue)
                {
                    if (disposing)
                    {
                        tokenSource.Dispose();
                        Exploration.Dispose();
                    }
                    disposedValue = true;
                }
            }
        }
    }
}
