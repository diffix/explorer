namespace Explorer.Api
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Explorer;

    public class ExplorationRegistry
    {
        private readonly Dictionary<Guid, Registration> registrations = new Dictionary<Guid, Registration>();

        public Guid Register(Task task, CancellationTokenSource tokenSource)
        {
            var id = Guid.NewGuid();
            registrations[id] = new Registration(task, tokenSource);

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
                await registration.Exploration;
            }
            finally
            {
                registration?.Dispose();
            }
        }

        public ExplorationStatus GetStatus(Guid id) => registrations[id].Status;

        public Task GetExplorationTask(Guid id) => registrations[id].Exploration;

        public void CancelExploration(Guid id)
        {
            registrations[id].CancelExploration();
        }

        private class Registration : IDisposable
        {
            private readonly CancellationTokenSource tokenSource;

            private bool disposedValue;

            public Registration(Task exploration, CancellationTokenSource tokenSource)
            {
                Exploration = exploration;
                this.tokenSource = tokenSource;
            }

            public Task Exploration { get; }

            public ExplorationStatus Status
            {
                get
                {
                    if (tokenSource.IsCancellationRequested)
                    {
                        return ExplorationStatus.Canceled;
                    }
                    return ConvertToExplorationStatus(Exploration.Status);
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

            private static ExplorationStatus ConvertToExplorationStatus(TaskStatus status)
            {
                return status switch
                {
                    TaskStatus.Canceled => ExplorationStatus.Canceled,
                    TaskStatus.Created => ExplorationStatus.New,
                    TaskStatus.Faulted => ExplorationStatus.Error,
                    TaskStatus.RanToCompletion => ExplorationStatus.Complete,
                    TaskStatus.Running => ExplorationStatus.Processing,
                    TaskStatus.WaitingForActivation => ExplorationStatus.Processing,
                    TaskStatus.WaitingToRun => ExplorationStatus.Processing,
                    TaskStatus.WaitingForChildrenToComplete => ExplorationStatus.Processing,
                    _ => throw new Exception("Unexpected TaskStatus: '{exploration.Status}'."),
                };
            }
        }
    }
}