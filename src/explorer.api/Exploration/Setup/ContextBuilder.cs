namespace Explorer.Api
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    using Aircloak.JsonApi;
    using Diffix;
    using Explorer.Common;

    public class ContextBuilder
    {
        private readonly JsonApiClient apiClient;

        public ContextBuilder(JsonApiClient apiClient)
        {
            this.apiClient = apiClient;
        }

        public async Task<IEnumerable<ExplorerContext>> Build(Uri apiUrl, string dataSource, string table, IEnumerable<string> columns)
        {
            var dataSources = await apiClient.GetDataSources(apiUrl, CancellationToken.None);

            if (!dataSources.AsDict.TryGetValue(dataSource, out var dataSourceInfo))
            {
                throw new MetaDataCheckException($"Could not find datasource '{dataSource}'.");
            }

            if (!dataSourceInfo.TableDict.TryGetValue(table, out var tableInfo))
            {
                throw new MetaDataCheckException($"Could not find table '{dataSource}.{table}'.");
            }

            return columns.Select(column =>
            {
                if (!tableInfo.ColumnDict.TryGetValue(column, out var columnInfo))
                {
                    throw new MetaDataCheckException($"Could not find column '{dataSource}.{table}.{column}'.");
                }
                return new CheckedContext(dataSource, table, column, columnInfo.Type);
            });
        }

        /// <summary>
        /// An <see cref="ExplorerContext" /> that has been checked to make sure the datasource,
        /// table and column exist on the database.
        /// </summary>
        private class CheckedContext : ExplorerContext
        {
            internal CheckedContext(string dataSource, string table, string column, DValueType columnType)
            {
                DataSource = dataSource;
                Table = new DSqlObjectName(table);
                Column = new DSqlObjectName(column);
                ColumnType = columnType;
            }

            public string DataSource { get; }

            public DSqlObjectName Table { get; }

            public DSqlObjectName Column { get; }

            public DValueType ColumnType { get; }
        }
    }
}
