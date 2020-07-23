namespace Explorer
{
    using System.Collections.Generic;
    using System.Linq;

    using Lamar;

    public class ExplorationLauncher
    {
        private readonly IContainer rootContainer;

        public ExplorationLauncher(IContainer rootContainer)
        {
            this.rootContainer = rootContainer;
        }

        /// <summary>
        /// Configure and launch an exploration.
        /// </summary>
        /// <param name="dataSource">The data source name on which to execute the exploration.</param>
        /// <param name="table">The table name on which to execute the exploration.</param>
        /// <param name="configurators">The ExplorationConfigurators for the columns.</param>
        /// <returns>A new Exploration object containing a running exploration.</returns>
        public Exploration LaunchExploration(
            string dataSource,
            string table,
            IEnumerable<ExplorationConfigurator> configurators)
        {
            var columnScopes = configurators.Select(configurator =>
            {
                var scope = new ExplorationScope(rootContainer.GetNestedContainer());
                configurator.Configure(scope);

                return scope;
            });

            var exploration = new Exploration(dataSource, table, columnScopes);
            exploration.Run();

            return exploration;
        }
    }
}
