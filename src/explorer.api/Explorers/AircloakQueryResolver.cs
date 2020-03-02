namespace Explorer
{
    using System;
    using System.Threading.Tasks;

    using Aircloak.JsonApi;
    using Aircloak.JsonApi.ResponseTypes;

    internal class AircloakQueryResolver : IQueryResolver
    {
        public AircloakQueryResolver(JsonApiClient apiClient, string dataSourceName)
        {
            ApiClient = apiClient;
            DataSourceName = dataSourceName;
        }

        public string DataSourceName { get; }

        public JsonApiClient ApiClient { get; }

        public async Task<QueryResult<TResult>> ResolveQuery<TResult>(IQuerySpec<TResult> query, TimeSpan timeout)
        {
            return await ApiClient.Query(
                DataSourceName,
                query,
                timeout);
        }
    }
}