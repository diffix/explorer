namespace Explorer.Api
{
    using System.Threading;
    using System.Threading.Tasks;

    using Aircloak.JsonApi;
    using Diffix;
    using Explorer.Common;

    internal class ContextBuilder
    {
        private readonly JsonApiClient apiClient;

        public ContextBuilder(JsonApiClient apiClient)
        {
            this.apiClient = apiClient;
        }

        public async Task<ExplorerContext> Build(Models.ExploreParams data)
        {
            var dataSources = await apiClient.GetDataSources(CancellationToken.None);

            if (!dataSources.AsDict.TryGetValue(data.DataSourceName, out var exploreDataSource))
            {
                throw new MetaDataCheckException($"Could not find datasource '{data.DataSourceName}'.");
            }

            if (!exploreDataSource.TableDict.TryGetValue(data.TableName, out var exploreTableMeta))
            {
                throw new MetaDataCheckException($"Could not find table '{data.TableName}'.");
            }

            if (!exploreTableMeta.ColumnDict.TryGetValue(data.ColumnName, out var exploreColumnMeta))
            {
                throw new MetaDataCheckException($"Could not find column '{data.ColumnName}'.");
            }

            return new CheckedContext(data, exploreColumnMeta.Type);
        }

        /// <summary>
        /// An <see cref="ExplorerContext" /> that has been checked to make sure the datasource,
        /// table and column exist on the database. 
        /// </summary>
        private class CheckedContext : ExplorerContext
        {
            internal CheckedContext(Models.ExploreParams data, DValueType columnType)
            {
                DataSource = data.DataSourceName;
                Table = data.TableName;
                Column = data.ColumnName;
                ColumnType = columnType;
            }

            public string DataSource { get; }

            public string Table { get; }

            public string Column { get; }

            public DValueType ColumnType { get; }
        }
    }
}
