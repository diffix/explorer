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

        public async Task<IEnumerable<ExplorerContext>> Build(AircloakConnection connection, Uri apiUrl, string dataSource, string table, IEnumerable<string> columns)
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
                var ci = new DColumnInfo(columnInfo.Type, columnInfo.UserId, columnInfo.Isolating.IsIsolator);
                return new CheckedContext(connection, dataSource, table, column, ci);
            });
        }

        /// <summary>
        /// An <see cref="ExplorerContext" /> that has been checked to make sure the datasource,
        /// table and column exist on the database.
        /// </summary>
        private class CheckedContext : ExplorerContext
        {
            private readonly string quotedTable;
            private readonly string quotedColumn;

            internal CheckedContext(AircloakConnection connection, string dataSource, string table, string column, DColumnInfo columnInfo)
            {
                Connection = connection;
                DataSource = dataSource;
                Table = table;
                Column = column;
                ColumnInfo = columnInfo;
                quotedTable = Quote(table);
                quotedColumn = Quote(column);
            }

            public AircloakConnection Connection { get; }

            public string DataSource { get; }

            public string Table { get; }

            public string Column { get; }

            public DColumnInfo ColumnInfo { get; }

            public Task<DResult<TRow>> Exec<TRow>(DQuery<TRow> query) =>
                Connection.Exec(query.BuildQueryStatement(quotedTable, quotedColumn), query.ParseRow);

            private static string Quote(string name) =>
                "\"" + name + "\"";
        }
    }
}
