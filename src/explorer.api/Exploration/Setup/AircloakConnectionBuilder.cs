namespace Explorer.Api
{
    using System;
    using System.Threading;

    using Aircloak.JsonApi;
    using Microsoft.Extensions.Options;

    public class AircloakConnectionBuilder
    {
        private readonly JsonApiClient apiClient;
        private readonly IOptions<ConnectionOptions> options;

        public AircloakConnectionBuilder(
            JsonApiClient apiClient,
            IOptions<ConnectionOptions> options)
        {
            this.options = options;
            this.apiClient = apiClient;
        }

        public AircloakConnection Build(Uri apiUri, string dataSource, CancellationToken token)
        {
            return new AircloakConnection(
                apiClient,
                apiUri,
                dataSource,
                options,
                token);
        }
    }
}
