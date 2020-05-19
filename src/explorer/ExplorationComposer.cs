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

        public void AddPublisher<T>()
            where T : PublisherComponent
        {
            if (scope.GetInstance<T>() is PublisherComponent publisherComponent)
            {
                var metricsPublisher = scope.GetInstance<MetricsPublisher>();

                tasks.Add(Task.Run(async () => await publisherComponent.PublishMetrics(metricsPublisher)));
            }
            else
            {
                throw new Exception($"Unable to resolve {typeof(T)}");
            }
        }

        internal Exploration Finalize() => new Exploration(Task.WhenAll(tasks));
    }
}
