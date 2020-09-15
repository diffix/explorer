namespace Explorer.Api
{
    using System;
    using System.Collections.Concurrent;
    using Explorer;
    using Explorer.Api.Models;
    using Microsoft.Extensions.Logging;

    using static ExplorationStatusEnum;

    public class ExplorationRegistry
    {
        private readonly ConcurrentDictionary<Guid, Registration> registrations =
            new ConcurrentDictionary<Guid, Registration>();

        public ExplorationRegistry(ILogger<ExplorationRegistry> logger)
        {
            Logger = logger;
        }

        public ILogger<ExplorationRegistry> Logger { get; }

        public Guid Register(ExploreParams requestData, Exploration exploration)
        {
            var id = Guid.NewGuid();

            registrations[id] = new Registration(requestData, exploration);

            return id;
        }

        public void Remove(Guid id)
        {
            if (!registrations.TryRemove(id, out var registration))
            {
                return;
            }

            if (!registration.Exploration.Status.IsComplete())
            {
                throw new InvalidOperationException("Exploration should not be removed before completion.");
            }

            registration.Dispose();
        }

        public bool IsRegistered(Guid id) => registrations.ContainsKey(id);

        public (ExploreParams, Exploration) GetExploration(Guid id)
        {
            var reg = registrations[id];
            return (reg.RequestData, reg.Exploration);
        }

        public void CancelExploration(Guid id)
        {
            registrations[id].Exploration.CancelExploration();
        }

        private class Registration : IDisposable
        {
            private bool disposedValue;

            public Registration(ExploreParams requestData, Exploration exploration)
            {
                RequestData = requestData;
                Exploration = exploration;
            }

            public ExploreParams RequestData { get; }

            public Exploration Exploration { get; }

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
                        Exploration.Dispose();
                    }
                    disposedValue = true;
                }
            }
        }
    }
}
