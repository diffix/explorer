namespace Explorer.Api
{
    using System;
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

        public DConnection Build(Models.ExploreParams data, CancellationToken token)
        {
            var url = data.ApiUrl.EndsWith("/")
                ? new Uri(data.ApiUrl)
                : new Uri($"{data.ApiUrl}/");

            return new AircloakConnection(
                apiClient,
                url,
                data.DataSourceName,
                explorerConfig.PollFrequencyTimeSpan,
                token);
        }
    }
}
