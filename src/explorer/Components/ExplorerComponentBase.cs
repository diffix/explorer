namespace Explorer.Components
{
    using Microsoft.Extensions.Logging;

    public abstract class ExplorerComponentBase
    {
#pragma warning disable CS8618 // Non-nullable property 'Context' is uninitialized. (property is set using Lamar DI)
        [Lamar.SetterProperty]
        public ExplorerContext Context { get; set; }

        [Lamar.SetterProperty]
        public ILogger Logger { get; set; }
#pragma warning restore CS8618
    }
}