namespace Explorer
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Diffix;
    using Explorer.Common;
    using Explorer.Components;
    using Explorer.Metrics;
    using Lamar;

    public class ExplorationConfig
    {
        private readonly INestedContainer scope;

        public ExplorationConfig(INestedContainer scope)
        {
            this.scope = scope;
        }

        public List<Task> Tasks { get; } = new List<Task>();

        public void AddPublisher<T>(Action<T>? configure = null)
            where T : PublisherComponent
        {
            var component = scope.ResolvePublisherComponent<T>();

            configure?.Invoke(component);

            var metricsPublisher = scope.GetInstance<MetricsPublisher>();

            Tasks.Add(component.PublishMetrics(metricsPublisher));
        }

        public void UseContext(ExplorerContext ctx) => scope.Inject(ctx);

        public ExplorationConfig Compose(Action<ExplorationConfig> action)
        {
            action(this);
            return this;
        }
    }
}
