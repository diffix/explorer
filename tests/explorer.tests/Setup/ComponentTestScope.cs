namespace Explorer.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Explorer.Components;
    using Explorer.Metrics;

    public class ComponentTestScope : QueryableTestScope
    {
        public ComponentTestScope(TestScope testScope)
        : base(testScope)
        {
        }

        public async Task Test<TComponent, TResult>(Action<TResult> test)
        where TComponent : ExplorerComponent<TResult>
        {
            var c = Inner.Scope.GetInstance<TComponent>();
            var result = await c.ResultAsync;

            test(result);
        }

        public async Task Test<T>(Action<IEnumerable<ExploreMetric>> test)
        where T : PublisherComponent
        {
            var p = Inner.Scope.GetInstance<T>();

            var metrics = new List<ExploreMetric>();
            await foreach (var m in p.YieldMetrics())
            {
                metrics.Add(m);
            }

            test(metrics);
        }
    }
}
