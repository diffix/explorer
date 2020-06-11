namespace Explorer.Api.Tests
{
    using Lamar;
    using Lamar.IoC;

    /// <summary>
    /// Lamar by default doesn't register injected instances for disposal. This extension fixes this by adding
    /// a new method to explicitly inject a disposable.
    /// </summary>
    public static class LamarExtensions
    {
        public static void InjectDisposable<T>(this INestedContainer nc, T instance, bool replace = false)
        {
            nc.Inject(instance, replace);
            ((Scope)nc).TryAddDisposable(instance);
        }
    }
}