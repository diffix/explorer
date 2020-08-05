namespace Explorer.Api
{
    using System;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    using Aircloak.JsonApi;
    using Explorer.Api.Authentication;
    using Lamar;

    public class ExplorationLauncher
    {
        private readonly IContainer rootContainer;
        private readonly ContextBuilder contextBuilder;

        public ExplorationLauncher(IContainer rootContainer, ContextBuilder contextBuilder)
        {
            this.contextBuilder = contextBuilder;
            this.rootContainer = rootContainer;
        }

        /// <summary>
        /// Configure and launch an exploration.
        /// </summary>
        /// <param name="requestData">The <see cref="Models.ExploreParams"/> submitted via the api.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the exploration.</param>
        /// <returns>A new Exploration object containing a running exploration.</returns>
        public async Task<Exploration> ValidateAndLaunch(
            Models.ExploreParams requestData,
            CancellationToken cancellationToken)
        {
            // Register the authentication token for this scope.
            if (rootContainer.GetInstance<IAircloakAuthenticationProvider>() is ExplorerApiAuthProvider auth)
            {
                auth.RegisterApiKey(requestData.ApiKey);
            }

            var apiUri = new Uri(requestData.ApiUrl);
            var ctxList = await contextBuilder.Build(requestData, cancellationToken);

            var columnScopes = ctxList.Select(ctx =>
            {
                var configurator = new ComponentComposition(ctx);
                var scope = new ExplorationScope(rootContainer.GetNestedContainer());
                configurator.Configure(scope);

                return scope;
            });

            var exploration = new Exploration(requestData.DataSource, requestData.Table, columnScopes);
            exploration.Run();

            return exploration;
        }
    }
}
