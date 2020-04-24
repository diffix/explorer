namespace Explorer.Common
{
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using Diffix;
    using Explorer.Explorers.Components;
    using Explorer.Explorers.Metrics;

    internal abstract class ExplorerBase : MetricsPublisher
    {
        private readonly List<Task> componentTasks = new List<Task>();

        private readonly ConcurrentBag<ExploreMetric> metrics = new ConcurrentBag<ExploreMetric>();

        protected ExplorerBase() { }

        protected ExplorerBase(DConnection conn, ExplorerContext ctx)
        {
            Conn = conn;
            Ctx = ctx;
        }

        public DConnection Conn { get; }

        public ExplorerContext Ctx { get; }

        public IEnumerable<ExploreMetric> Metrics => metrics.ToArray();

        public async Task Explore()
        {
            InitializeComponents();
            await Task.WhenAll(componentTasks);
        }

        public virtual Task Explore(DConnection conn, ExplorerContext ctx)
        {
            return Explore();
        }

        protected virtual void InitializeComponents() { }

        public virtual void PublishMetric(ExploreMetric metric) =>
            metrics.Add(metric);

        protected async Task RunAndPublish<T>(ExplorerComponent<T> component)
        {
            var result = await component.ResultAsync;
            if (result is MetricsProvider provider && this is MetricsPublisher publisher)
            {
                await publisher.PublishMetricsAsync(provider);
            }
        }

        protected TComponent Initialize<TComponent, TResult>()
            where TComponent : ExplorerComponent<TResult>
        {
            var instance = typeof(TComponent)
                .GetConstructor(new[] { typeof(DConnection), typeof(ExplorerContext) })
                ?.Invoke(new object[] { Conn, Ctx })
                ?? throw new System.Exception("Expected an instance of ExplorerComponent with a constructor");

            if (instance is TComponent component)
            {
                componentTasks.Add(RunAndPublish(component));
                return component;
            }

            throw new System.Exception("Unable to create an instance of ExplorerComponent.");
        }
    }
}