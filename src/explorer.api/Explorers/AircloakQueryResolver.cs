namespace Explorer
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    using Aircloak.JsonApi;
    using Aircloak.JsonApi.ResponseTypes;

    internal class AircloakQueryResolver : IQueryResolver
    {
        private readonly string dataSourceName;

        private readonly JsonApiClient apiClient;

        private readonly TimeSpan pollFrequency;

        public AircloakQueryResolver(JsonApiClient apiClient, string dataSourceName, TimeSpan pollFrequency)
        {
            this.apiClient = apiClient;
            this.dataSourceName = dataSourceName;
            this.pollFrequency = pollFrequency;
        }

        public async Task<QueryResult<TResult>> ResolveQuery<TResult>(IQuerySpec<TResult> query, CancellationToken ct)
        {
            return await apiClient.Query(
                dataSourceName,
                query,
                pollFrequency,
                ct);
        }
    }
}