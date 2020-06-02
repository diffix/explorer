namespace Explorer.Api
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    using Aircloak.JsonApi;
    using Diffix;
    using Explorer;
    using Explorer.Api.Authentication;
    using Explorer.Components;
    using Lamar;

    public class ExplorationLauncher
    {
        private readonly IContainer rootContainer;

        public ExplorationLauncher(IContainer rootContainer)
        {
            this.rootContainer = rootContainer;
        }

        /// <summary>
        /// Configures and runs an Exploration.
        /// </summary>
        /// <param name="scope">This should be a fresh nested scope based off the main container.</param>
        /// <param name="data">The params of the current explorer api request.</param>
        /// <param name="ct">A cancellation token that will be passed to subtasks.</param>
        /// <returns>A task that represents the configured Exploration.</returns>
        public static async Task Explore(INestedContainer scope, Models.ExploreParams data, CancellationToken ct)
        {
            // Register the authentication token for this scope.
            if (scope.GetInstance<IAircloakAuthenticationProvider>() is ExplorerApiAuthProvider auth)
            {
                auth.RegisterApiKey(data.ApiKey);
            }

            // Create the Context and Connection objects for this exploration.
            var ctx = await scope.GetInstance<ContextBuilder>().Build(data);
            var conn = scope.GetInstance<AircloakConnectionBuilder>().Build(data.DataSourceName, ct);

            // Configure a new Exploration
            var exploration = Exploration.Configure(scope, _ =>
            {
                _.UseConnection(conn);
                _.UseContext(ctx);
                _.Compose(ConfigureComponents(ctx.ColumnType));
            });

            // Run and await completion of all components
            await exploration.Completion;
        }

        /// <summary>
        /// Runs the exploration as a background task.
        /// </summary>
        /// <param name="data">The params of the current explorer api request.</param>
        /// <param name="ct">A cancellation token that will be passed to subtasks.</param>
        /// <returns>The running Task.</returns>
        public Task LaunchExploration(Models.ExploreParams data, CancellationToken ct) =>
            Task.Run(async () => await Explore(data, ct));

        private static Action<ExplorationConfig> ConfigureComponents(DValueType columnType) =>
            columnType switch
            {
                DValueType.Integer => NumericExploration,
                DValueType.Real => NumericExploration,
                DValueType.Text => TextExploration,
                DValueType.Timestamp => DatetimeExploration,
                DValueType.Date => DatetimeExploration,
                DValueType.Datetime => DatetimeExploration,
                DValueType.Bool => _ => _.AddPublisher<DistinctValuesComponent>(),
                DValueType.Unknown => throw new ArgumentException(
                    $"Cannot explore column type {columnType}.", nameof(columnType)),
            };

        private static void NumericExploration(ExplorationConfig config)
        {
            config.AddPublisher<NumericHistogramComponent>();
            config.AddPublisher<QuartileEstimator>();
            config.AddPublisher<AverageEstimator>();
            config.AddPublisher<MinMaxRefiner>();
            config.AddPublisher<DistinctValuesComponent>();
        }

        private static void TextExploration(ExplorationConfig config)
        {
            config.AddPublisher<DistinctValuesComponent>();
            config.AddPublisher<EmailCheckComponent>();
            config.AddPublisher<TextGeneratorComponent>();
            config.AddPublisher<TextLengthComponent>();
        }

        private static void DatetimeExploration(ExplorationConfig config)
        {
            config.AddPublisher<DistinctValuesComponent>();
            config.AddPublisher<LinearTimeBuckets>();
            config.AddPublisher<CyclicalTimeBuckets>();
        }

        private async Task Explore(Models.ExploreParams data, CancellationToken ct)
        {
            // This scope (and all the components resolved within) should live until the end of the Task.
            using var scope = rootContainer.GetNestedContainer();

            await Explore(scope, data, ct);
        }
    }
}