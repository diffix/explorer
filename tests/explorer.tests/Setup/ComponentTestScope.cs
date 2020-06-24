namespace Explorer.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Explorer.Components;
    using Explorer.Metrics;

    using Xunit;

    public class ComponentTestScope : QueryableTestScope
    {
        public ComponentTestScope(TestScope testScope)
        : base(testScope)
        {
        }

        public async Task ResultTest<TComponent, TResult>(Action<TResult> test)
        where TComponent : ResultProvider<TResult>
        {
            // Resolve the component using the interface to ensure correct scope
            var c = Inner.Scope.GetInstance<ResultProvider<TResult>>();
            Assert.IsType<TComponent>(c);

            var result = await c.ResultAsync;

            test(result);
        }

        public async Task MetricsTest<T>(Action<IEnumerable<ExploreMetric>> test)
        where T : PublisherComponent
        {
            var publisher = Inner.Scope.ResolvePublisherComponent<T>();

            var metrics = new List<ExploreMetric>();
            await foreach (var m in publisher.YieldMetrics())
            {
                metrics.Add(m);
            }

            test(metrics);
        }

        public void ConfigurePublisher<T>(Action<T> doSomething)
        where T : PublisherComponent
        {
            var publisher = Inner.Scope.ResolvePublisherComponent<T>();
            doSomething(publisher);
        }
    }
}
