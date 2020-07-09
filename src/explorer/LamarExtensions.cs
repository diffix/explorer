namespace Explorer.Components
{
    using Lamar;
    using LamarCodeGeneration;

    public static class LamarExtensions
    {
        public static T ResolvePublisherComponent<T>(this INestedContainer scope)
            where T : PublisherComponent
        {
            // Try to resolve using the PublisherComponent interface. If this doesn't work, auto-resolve
            // the concrete instance instead.
            var fromCollection = (T)scope.TryGetInstance<PublisherComponent>(typeof(T).NameInCode());
          return fromCollection ?? scope.GetInstance<T>();
        }
    }
}
