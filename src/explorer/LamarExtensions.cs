namespace Explorer.Components
{
    using Lamar;
    using LamarCodeGeneration;

    public static class LamarExtensions
    {
        public static T ResolvePublisherComponent<T>(this INestedContainer scope)
            where T : PublisherComponent
        => (T)scope.GetInstance<PublisherComponent>(typeof(T).NameInCode());
    }
}