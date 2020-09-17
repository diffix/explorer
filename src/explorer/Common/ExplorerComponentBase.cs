namespace Explorer.Components
{
    public abstract class ExplorerComponentBase
    {
#pragma warning disable CS8618 // Non-nullable property 'Context' is uninitialized. (property is set using Lamar DI)
        [Lamar.SetterProperty]
        public ExplorerContext Context { get; set; }
#pragma warning restore CS8618
    }
}