namespace Explorer.Api
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Explorer;

    public class ExplorationRegistry
    {
        private readonly ConcurrentDictionary<Guid, Registration> registrations =
            new ConcurrentDictionary<Guid, Registration>();

        public Guid Register(Exploration exploration, CancellationTokenSource tokenSource)
        {
            var id = Guid.NewGuid();
            registrations[id] = new Registration(exploration, tokenSource);

            return id;
        }

        public async Task Remove(Guid id)
        {
            registrations.Remove(id, out var registration);

            try
            {
                if (registration is null)
                {
                    return;
                }
                registration.CancelExploration();

                // Wait for the task to finish before disposing the registration.
                // This will also allow any Exceptions to surface.
                await registration.Exploration.Completion;
            }
            finally
            {
                registration?.Dispose();
            }
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
                    }
                    disposedValue = true;
                }
            }
        }
    }
}