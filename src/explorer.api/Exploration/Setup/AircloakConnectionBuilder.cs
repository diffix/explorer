namespace Explorer.Api
{
    using System;
    using System.Threading;

    using Aircloak.JsonApi;

    public class AircloakConnectionBuilder
    {
        private readonly JsonApiClient apiClient;
        private readonly ExplorerConfig explorerConfig;

        public AircloakConnectionBuilder(JsonApiClient apiClient, ExplorerConfig explorerConfig)
        {
            this.explorerConfig = explorerConfig;
            this.apiClient = apiClient;
        }

        public AircloakConnection Build(Uri apiUri, string dataSource, CancellationToken token)
        {
            return new AircloakConnection(
                apiClient,
                apiUri,
                dataSource,
                explorerConfig.PollFrequencyTimeSpan,
                token);
        }
    }
}
