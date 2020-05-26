namespace Explorer.Tests
{
    using System;
    using System.Threading.Tasks;
    using Explorer.Components;

    public class ComponentTestScope : QueryableTestScope
    {
        public ComponentTestScope(TestScope testScope) : base(testScope)
        {
        }

        public async Task Test<TComponent, TResult>(Action<TResult> test)
        where TComponent : ExplorerComponent<TResult>
        {
            var c = Inner.Scope.GetInstance<TComponent>();
            var result = await c.ResultAsync;

            test(result);
        }
    }
}
