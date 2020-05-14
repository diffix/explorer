namespace Explorer.Api
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    using Aircloak.JsonApi;
    using Diffix;
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

        private static ExplorationTasks SelectComponents(INestedContainer scope, DValueType columnType) =>
            columnType switch
            {
                DValueType.Integer => new ExplorationTasks(scope)
                                        .AddNumericPublishers()
                                        .AddCategoricalPublishers(),
                DValueType.Real => new ExplorationTasks(scope)
                                        .AddNumericPublishers()
                                        .AddCategoricalPublishers(),
                DValueType.Text => throw new NotImplementedException(),
                DValueType.Timestamp => new ExplorationTasks(scope)
                                        .AddDatetimePublishers(),
                DValueType.Date => new ExplorationTasks(scope)
                                        .AddDatetimePublishers(),
                DValueType.Datetime => new ExplorationTasks(scope)
                                        .AddDatetimePublishers(),
                DValueType.Bool => new ExplorationTasks(scope)
                                        .AddCategoricalPublishers(),
                DValueType.Unknown => throw new NotImplementedException(),
            };

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

        private class ExplorationTasks : List<Task>
        {
            private readonly INestedContainer scope;

            public ExplorationTasks(INestedContainer scope)
            {
                this.scope = scope;
            }

            public Task Completion => Task.WhenAll(this);

            public ExplorationTasks AddPublisher<T>()
                where T : PublisherComponent
            {
                if (scope.GetInstance<T>() is PublisherComponent publisherComponent)
                {
                    Add(Task.Run(async () => await publisherComponent.PublishMetricsAsync()));
                    return this;
                }
                else
                {
                    throw new Exception($"Unable to resolve {typeof(T)}");
                }
            }

            public ExplorationTasks AddNumericPublishers() =>
                AddPublisher<HistogramPublisher>()
                .AddPublisher<QuartilesPublisher>()
                .AddPublisher<AveragePublisher>()
                .AddPublisher<MinMaxPublisher>();

            public ExplorationTasks AddCategoricalPublishers() =>
                AddPublisher<DistinctValuesPublisher>();

            public ExplorationTasks AddDatetimePublishers() =>
                AddPublisher<LinearTimeBucketsPublisher>();
        }
    }
}