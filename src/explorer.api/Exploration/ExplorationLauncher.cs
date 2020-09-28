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
        private readonly ExplorationScopeBuilder scopeBuilder;

        public ExplorationLauncher(
            IContainer rootContainer,
            ContextBuilder contextBuilder,
            ExplorationScopeBuilder scopeBuilder)
        {
            this.rootContainer = rootContainer;
            this.contextBuilder = contextBuilder;
            this.scopeBuilder = scopeBuilder;
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

            var contextList = await contextBuilder.Build(requestData, cancellationToken);

            var columnScopes = contextList.Select(
                context => scopeBuilder.Build(rootContainer.GetNestedContainer(), context));

            var exploration = new Exploration(requestData.DataSource, requestData.Table, columnScopes);
            exploration.Run();

            return exploration;
        }
    }
}
