namespace Explorer
{
    using Lamar;

    public abstract class ExplorationScopeBuilder
    {
        public ExplorationScope Build(INestedContainer scope, ExplorerContext context)
        {
            var explorationScope = new ExplorationScope(scope);
            explorationScope.UseContext(context);
            Configure(explorationScope, context);
            return explorationScope;
        }

        protected abstract void Configure(ExplorationScope scope, ExplorerContext context);
    }
}