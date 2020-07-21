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
                var ci = new ColumnInfo(columnInfo.Type, columnInfo.UserId, columnInfo.Isolating ?? true);
                return new CheckedContext(dataSource, table, column, ci);
            });
        }

        /// <summary>
        /// An <see cref="ExplorerContext" /> that has been checked to make sure the datasource,
        /// table and column exist on the database.
        /// </summary>
        private class CheckedContext : ExplorerContext
        {
            internal CheckedContext(string dataSource, string table, string column, ColumnInfo columnInfo)
            {
                DataSource = dataSource;
                Table = table;
                Column = column;
                ColumnInfo = columnInfo;
            }

            public string DataSource { get; }

            public string Table { get; }

            public string Column { get; }

            public ColumnInfo ColumnInfo { get; }
        }
    }
}
