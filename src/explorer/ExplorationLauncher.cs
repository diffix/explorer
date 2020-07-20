namespace Explorer
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Diffix;
    using Explorer.Common;
    using Explorer.Metrics;
    using Lamar;

    public class ExplorationLauncher
    {
        private readonly IContainer rootContainer;

        public ExplorationLauncher(IContainer rootContainer)
        {
            this.rootContainer = rootContainer;
        }

        /// <summary>
        /// Configure a column exploration within a given scope.
        /// </summary>
        /// <param name="scope">The scoped container to use for object resolution.</param>
        /// <param name="conn">A DConnection configured for the Api backend.</param>
        /// <param name="ctx">An <see cref="ExplorerContext" /> defining the exploration parameters.</param>
        /// <param name="componentConfiguration">
        /// An action to add and configure the components to use in this exploration.
        /// </param>
        /// <returns>A new ColumnExploration object.</returns>
        public static ColumnExploration ExploreColumn(
            INestedContainer scope,
            DConnection conn,
            ExplorerContext ctx,
            Action<ExplorationConfig> componentConfiguration)
        {
            // Configure a new Exploration
            var config = new ExplorationConfig(scope);
            config.UseConnection(conn);
            config.UseContext(ctx);
            config.Compose(componentConfiguration);
            return new ColumnExploration(config, scope, ctx.Column[1..^1]); // unquote column name
        }

        /// <summary>
        /// Configure a column exploration.
        /// </summary>
        /// <param name="conn">A DConnection configured for the Api backend.</param>
        /// <param name="ctx">An <see cref="ExplorerContext" /> defining the exploration parameters.</param>
        /// <param name="componentConfiguration">
        /// An action to add and configure the components to use in this exploration.
        /// </param>
        /// <returns>A new ColumnExploration object.</returns>
        public ColumnExploration LaunchColumnExploration(
            DConnection conn,
            ExplorerContext ctx,
            Action<ExplorationConfig> componentConfiguration)
        {
            // This scope (and all the components resolved within) should live until the end of the Task.
            return ExploreColumn(rootContainer.GetNestedContainer(), conn, ctx, componentConfiguration);
        }

        /// <summary>
        /// Configure an exploration.
        /// </summary>
        /// <param name="dataSource">The data source name on which to execute the exploration.</param>
        /// <param name="table">The table name on which to execute the exploration.</param>
        /// <param name="conn">A DConnection configured for the Api backend.</param>
        /// <param name="explorationSettings">
        /// A list of tuples containing the exploration parameters and
        /// the action to add and configure the components to use in this exploration.
        /// </param>
        /// <returns>A new Exploration object.</returns>
        public Exploration LaunchExploration(
            string dataSource,
            string table,
            DConnection conn,
            IEnumerable<(Action<ExplorationConfig> ComponentConfig, ExplorerContext Context)> explorationSettings)
        {
            // This scope (and all the components resolved within) should live until the end of the Task.
            var columnExplorations = explorationSettings.Select(item =>
                LaunchColumnExploration(conn, item.Context, item.ComponentConfig));
            return new Exploration(dataSource, table, columnExplorations.ToList());
        }
    }
}
