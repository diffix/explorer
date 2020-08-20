namespace Explorer.Api
{
    using Aircloak.JsonApi;
    using Explorer.Api.Authentication;

    public class ExplorationLauncher
    {
        private readonly ContextBuilder contextBuilder;
        private readonly Exploration exploration;
        private readonly IAircloakAuthenticationProvider authProvider;

        public ExplorationLauncher(
            Exploration exploration,
            IAircloakAuthenticationProvider authProvider,
            ContextBuilder contextBuilder)
        {
            this.authProvider = authProvider;
            this.exploration = exploration;
            this.contextBuilder = contextBuilder;
        }

        /// <summary>
        /// Configure and launch an exploration.
        /// </summary>
        /// <param name="requestData">The <see cref="Models.ExploreParams"/> submitted via the api.</param>
        /// <returns>A new Exploration object containing a running exploration.</returns>
        public Exploration Launch(Models.ExploreParams requestData)
        {
            // Register the authentication token for this scope.
            if (authProvider is ExplorerApiAuthProvider auth)
            {
                auth.RegisterApiKey(requestData.ApiKey);
            }

            exploration.Initialise(contextBuilder, requestData);
            exploration.Run();

            return exploration;
        }
    }
}
