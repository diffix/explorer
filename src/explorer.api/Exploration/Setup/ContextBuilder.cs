namespace Explorer.Api
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    using Aircloak.JsonApi;
    using Diffix;

    public class ContextBuilder
    {
        private readonly AircloakConnectionBuilder connectionBuilder;

        public ContextBuilder(AircloakConnectionBuilder connectionBuilder)
        {
            this.connectionBuilder = connectionBuilder;
        }

        public async Task<IEnumerable<ExplorerContext>> Build(
            Models.ExploreParams requestData,
            CancellationToken cancellationToken)
        {
            var connection = connectionBuilder.Build(
                new Uri(requestData.ApiUrl), requestData.DataSource, cancellationToken);

            var dataSources = await connection.GetDataSources();

            var (dataSource, table, columns) = (requestData.DataSource, requestData.Table, requestData.Columns);

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
            internal CheckedContext(AircloakConnection connection, string dataSource, string table, string column, DColumnInfo columnInfo)
            {
                Connection = connection;
                DataSource = dataSource;
                Table = table;
                Column = column;
                ColumnInfo = columnInfo;
            }

            public AircloakConnection Connection { get; }

            public string DataSource { get; }

            public string Table { get; }

            public string Column { get; }

            public DColumnInfo ColumnInfo { get; }

            public Task<DResult<TRow>> Exec<TQuery, TRow>(TQuery query)
            where TQuery : DQueryStatement, DResultParser<TRow> =>
                Connection.Exec(query.BuildQueryStatement(Table, Column), query.ParseRow);

            public Task<DResult<TRow>> Exec<TRow>(DQuery<TRow> query) =>
                Connection.Exec(query.BuildQueryStatement(Table, Column), query.ParseRow);
        }
    }
}
