namespace Explorer
{
    using System;
    using System.Threading.Tasks;
    using Lamar;

    public sealed class Exploration
    {
        public Exploration(ExplorationConfig config)
        {
            Completion = Task.WhenAll(config.Tasks);
        }

        public Task Completion { get; }

        public static Exploration Configure(INestedContainer scope, Action<ExplorationConfig> configure)
        {
            var config = new ExplorationConfig(scope);
            configure(config);
            return new Exploration(config);
        }
    }
}