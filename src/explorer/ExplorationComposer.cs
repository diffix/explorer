namespace Explorer
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using Explorer.Components;
    using Explorer.Metrics;
    using Lamar;

    public class ExplorationComposer
    {
        private readonly List<Task> tasks = new List<Task>();

        private readonly INestedContainer scope;

        internal ExplorationComposer(INestedContainer scope)
        {
            this.scope = scope;
        }

        public void AddPublisher<T>(Action<T>? configure = null)
            where T : PublisherComponent
        {
            var component = scope.GetInstance<T>();
            configure?.Invoke(component);

            var metricsPublisher = scope.GetInstance<MetricsPublisher>();

            tasks.Add(Task.Run(async () => await component.PublishMetrics(metricsPublisher)));
        }

        internal Exploration Finalize() => new Exploration(Task.WhenAll(tasks));
    }
}
