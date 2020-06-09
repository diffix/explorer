namespace Explorer.Api
{
    using System;
    using System.Threading.Tasks;

    using Diffix;
    using Explorer;
    using Explorer.Common;
    using Lamar;

    public class ExplorationLauncher
    {
        private readonly IContainer rootContainer;

        public ExplorationLauncher(IContainer rootContainer)
        {
            this.rootContainer = rootContainer;
        }

        /// <summary>
        /// Runs the exploration as a background task.
        /// </summary>
        /// <param name="ctx">An <see cref="ExplorerContext" /> defining the exploration parameters.</param>
        /// <param name="conn">A DConnection configured for the Api backend.</param>
        /// <param name="componentConfiguration">
        /// An action to add and configure the components to use in this exploration.
        /// </param>
        /// <returns>The running Task.</returns>
        public async Task LaunchExploration(
            ExplorerContext ctx,
            DConnection conn,
            Action<ExplorationConfig> componentConfiguration)
        {
            // This scope (and all the components resolved within) should live until the end of the Task.
            using var scope = rootContainer.GetNestedContainer();

            // Configure a new Exploration
            var exploration = Exploration.Configure(scope, _ =>
            {
                _.UseConnection(conn);
                _.UseContext(ctx);
                _.Compose(componentConfiguration);
            });

            // Run and await completion of all components
            await exploration.Completion;
        }
    }
}
