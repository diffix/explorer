namespace Explorer.Api
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Explorer;
    using Explorer.Api.Models;
    using Microsoft.Extensions.Logging;

    using static ExplorationStatusEnum;

    public class ExplorationRegistry
    {
        private readonly ConcurrentDictionary<Guid, Registration> registrations =
            new ConcurrentDictionary<Guid, Registration>();

        private readonly ExplorationLauncher launcher;

        public ExplorationRegistry(ExplorationLauncher launcher, ILogger<ExplorationRegistry> logger)
        {
            this.launcher = launcher;
            Logger = logger;
        }

        public ILogger<ExplorationRegistry> Logger { get; }

        public Guid Register(ExploreParams requestData)
        {
            var id = Guid.NewGuid();
            registrations[id] = new Registration(launcher, requestData);

            return id;
        }

        public void Remove(Guid id)
        {
            if (!registrations.TryRemove(id, out var registration))
            {
                return;
            }

            if (!registration.Status.IsComplete())
            {
                throw new InvalidOperationException("Exploration should not be removed before completion.");
            }

            registration.Dispose();
        }

        public bool IsRegistered(Guid id) => registrations.ContainsKey(id);

        public ExplorationStatus GetStatus(Guid id) => registrations[id].Status;

        public ExploreParams GetExplorationParams(Guid id) => registrations[id].RequestData;

        public Exploration? GetExploration(Guid id) => registrations[id].Exploration;

        public IEnumerable<string> GetValidationErrors(Guid id) => registrations[id].ValidationErrors;

        public void CancelExploration(Guid id)
        {
            registrations[id].CancelExploration();
        }

        private class Registration : IDisposable
        {
            private readonly CancellationTokenSource tokenSource = new CancellationTokenSource();

            private bool disposedValue;

            public Registration(ExplorationLauncher launcher, ExploreParams requestData)
            {
                RequestData = requestData;
                ValidationTask = Task.Run(async () =>
                    Exploration = await launcher.ValidateAndLaunch(requestData, tokenSource.Token));
            }

            public ExploreParams RequestData { get; }

            public Task<Exploration> ValidationTask { get; }

            public Exploration? Exploration { get; private set; }

            public ExplorationStatus Status
            {
                get
                {
                    if (tokenSource.IsCancellationRequested)
                    {
                        return ExplorationStatus.Canceled;
                    }
                    if (ValidationTask.IsCompletedSuccessfully)
                    {
                        return Exploration!.Status;
                    }
                    if (!ValidationTask.IsCompleted)
                    {
                        return ExplorationStatus.Validating;
                    }
                    return ValidationTask.Status.ToExplorationStatus();
                }
            }

            public IEnumerable<string> ValidationErrors =>
                ValidationTask.Exception?.Flatten().InnerExceptions.Select(ex => ex.Message) ?? Array.Empty<string>();

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
                        Exploration?.Dispose();
                    }
                    disposedValue = true;
                }
            }
        }
    }
}
