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

        public Task LaunchExploration(Models.ExploreParams data, CancellationToken ct) =>
            Task.Run(async () => await Explore(data, ct));

        private static Exploration SelectComponents(INestedContainer scope, DValueType columnType) =>
            columnType switch
            {
                DValueType.Integer => NumericExploration(scope),
                DValueType.Real => NumericExploration(scope),
                DValueType.Text => TextExploration(scope),
                DValueType.Timestamp => DatetimeExploration(scope),
                DValueType.Date => DatetimeExploration(scope),
                DValueType.Datetime => DatetimeExploration(scope),
                DValueType.Bool => Exploration.Compose(scope, _ => _.AddPublisher<DistinctValuesComponent>()),
                DValueType.Unknown => throw new NotImplementedException(),
            };

        private static Exploration NumericExploration(INestedContainer scope) =>
            Exploration.Compose(scope, _ =>
            {
                _.AddPublisher<NumericHistogramComponent>();
                _.AddPublisher<QuartileEstimator>();
                _.AddPublisher<AverageEstimator>();
                _.AddPublisher<MinMaxRefiner>();
                _.AddPublisher<DistinctValuesComponent>();
            });

        private static Exploration TextExploration(INestedContainer scope) =>
            Exploration.Compose(scope, _ =>
            {
                _.AddPublisher<TextLengthComponent>();
            });

        private static Exploration DatetimeExploration(INestedContainer scope) =>
            Exploration.Compose(scope, _ =>
            {
                _.AddPublisher<LinearTimeBuckets>();
            });

        private async Task Explore(Models.ExploreParams data, CancellationToken ct)
        {
            // This scope (and all the components resolved within) should live until the end of the Task.
            using var scope = rootContainer.GetNestedContainer();

            // Register the authentication token for this scope.
            if (scope.GetInstance<IAircloakAuthenticationProvider>() is ExplorerApiAuthProvider auth)
            {
                auth.RegisterApiKey(data.ApiKey);
            }

            // Create the Context and Connection objects for this exploration and inject them into the scope.
            var ctx = await scope.GetInstance<ContextBuilder>().Build(data);
            var conn = scope.GetInstance<AircloakConnectionBuilder>().Build(data.DataSourceName, ct);

            scope.Inject(ctx);
            scope.Inject(conn);

            // Choose components based on column type.
            var explorationTasks = SelectComponents(scope, ctx.ColumnType);

            // Run and await completion of all components
            await explorationTasks.Completion;
        }
    }
}