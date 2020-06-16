namespace Explorer
{
    using System;
    using System.Threading.Tasks;

    using Diffix;
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
        /// Configure an exploration within a given scope.
        /// </summary>
        /// <param name="scope">The scoped container to use for object resolution.</param>
        /// <param name="ctx">An <see cref="ExplorerContext" /> defining the exploration parameters.</param>
        /// <param name="conn">A DConnection configured for the Api backend.</param>
        /// <param name="componentConfiguration">
        /// An action to add and configure the components to use in this exploration.
        /// </param>
        /// <returns>The running Task.</returns>
        public static Exploration Explore(
            INestedContainer scope,
            ExplorerContext ctx,
            DConnection conn,
            Action<ExplorationConfig> componentConfiguration)
        {
            // Configure a new Exploration
            return Exploration.Configure(scope, _ =>
            {
                _.UseConnection(conn);
                _.UseContext(ctx);
                _.Compose(componentConfiguration);
            });
        }

        /// <summary>
        /// Configure an exploration.
        /// </summary>
        /// <param name="ctx">An <see cref="ExplorerContext" /> defining the exploration parameters.</param>
        /// <param name="conn">A DConnection configured for the Api backend.</param>
        /// <param name="componentConfiguration">
        /// An action to add and configure the components to use in this exploration.
        /// </param>
        /// <returns>The running Task.</returns>
        public Exploration LaunchExploration(
            ExplorerContext ctx,
            DConnection conn,
            Action<ExplorationConfig> componentConfiguration)
        {
            // This scope (and all the components resolved within) should live until the end of the Task.
            using var scope = rootContainer.GetNestedContainer();
            return Explore(scope, ctx, conn, componentConfiguration);
        }
    }
}
