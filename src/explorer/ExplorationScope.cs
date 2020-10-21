namespace Explorer
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

        public List<Func<Task>> Tasks { get; } = new List<Func<Task>>();

        public MetricsPublisher MetricsPublisher { get => scope.GetInstance<MetricsPublisher>(); }

        public ExplorerContext Context { get => scope.GetInstance<ExplorerContext>(); }

        public ILogger Logger { get => logger; }

        /// <summary>
        /// Register a publisher component.
        /// </summary>
        /// <param name="configure">A configuration action to be run after the component is created.</param>
        /// <param name="initialise">An initialisation action to be run before the component is run.</param>
        /// <typeparam name="T">Must be a component that implements <see cref="PublisherComponent" />.</typeparam>
        public void AddPublisher<T>(Action<T>? configure = null, Action<T>? initialise = null)
            where T : PublisherComponent
        {
            var component = scope.ResolvePublisherComponent<T>();
            var publisher = MetricsPublisher;

            configure?.Invoke(component);

            Tasks.Add(async () =>
            {
                initialise?.Invoke(component);
                await component.PublishMetrics(publisher);
            });
        }

        public void UseContext(ExplorerContext context) => scope.Inject(context);

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
