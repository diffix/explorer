namespace Explorer.Api
{
    using System.Threading;

    using Aircloak.JsonApi;
    using Diffix;

    public class AircloakConnectionBuilder
    {
        private readonly JsonApiClient apiClient;
        private readonly ExplorerConfig explorerConfig;

        public AircloakConnectionBuilder(JsonApiClient apiClient, ExplorerConfig explorerConfig)
        {
            this.explorerConfig = explorerConfig;
            this.apiClient = apiClient;
        }

        public DConnection Build(string dataSourceName, CancellationToken token)
        {
            return new AircloakConnection(apiClient, dataSourceName, explorerConfig.PollFrequencyTimeSpan, token);
        }
    }
}
