﻿namespace Explorer
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using Explorer.Components;
    using Explorer.Metrics;
    using Lamar;
    using Microsoft.Extensions.Logging;

    public class ExplorationScope : IDisposable
    {
        private readonly INestedContainer scope;

        private readonly ILogger<ExplorationScope> logger;

        private bool disposedValue;

        public ExplorationScope(INestedContainer scope)
        {
            this.scope = scope;
            logger = scope.GetInstance<ILogger<ExplorationScope>>();
        }

        public List<Task> Tasks { get; } = new List<Task>();

        public MetricsPublisher MetricsPublisher { get => scope.GetInstance<MetricsPublisher>(); }

        public ExplorerContext? Ctx { get => scope.TryGetInstance<ExplorerContext>(); }

        public ILogger Logger { get => logger; }

        public void AddPublisher<T>(Action<T>? configure = null)
            where T : PublisherComponent
        {
            var component = scope.ResolvePublisherComponent<T>();

            configure?.Invoke(component);

            Tasks.Add(component.PublishMetrics(MetricsPublisher));
        }

        public void UseContext(ExplorerContext ctx) => scope.Inject(ctx);

        public void Configure(ExplorationConfigurator configurator) => configurator.Configure(this);

        public ColumnExploration Build()
        {
            if (Ctx is null)
            {
                throw new InvalidOperationException(
                    $"Can't build {nameof(ColumnExploration)} without a context object!");
            }

            return new ColumnExploration(this);
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    scope.Dispose();
                }
                disposedValue = true;
            }
        }
    }
}
