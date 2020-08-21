namespace Explorer.Api
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    using Aircloak.JsonApi;
    using Diffix;

    public class JsonApiContextBuilder : ExplorerContextBuilder<Models.ExploreParams>
    {
        private readonly AircloakConnectionBuilder connectionBuilder;

        public JsonApiContextBuilder(AircloakConnectionBuilder connectionBuilder)
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
            internal CheckedContext(
                AircloakConnection connection,
                string dataSource,
                string table,
                string column,
                DColumnInfo columnInfo)
            {
                Connection = connection;
                DataSource = dataSource;
                Table = table;
                Columns = ImmutableArray.Create(column);
                ColumnInfos = ImmutableArray.Create(columnInfo);
            }

            internal CheckedContext(
                AircloakConnection connection,
                string dataSource,
                string table,
                IEnumerable<string> columns,
                IEnumerable<DColumnInfo> columnInfos)
            {
                Connection = connection;
                DataSource = dataSource;
                Table = table;
                Columns = ImmutableArray.CreateRange(columns);
                ColumnInfos = ImmutableArray.CreateRange(columnInfos);
            }

            public AircloakConnection Connection { get; }

            public string DataSource { get; }

            public string Table { get; }

            public ImmutableArray<string> Columns { get; }

            public ImmutableArray<DColumnInfo> ColumnInfos { get; }

            public Task<DResult<TRow>> Exec<TQuery, TRow>(TQuery query)
            where TQuery : DQueryStatement, DResultParser<TRow> =>
                Connection.Exec(query.BuildQueryStatement(Table, Columns.ToArray()), query.ParseRow);

            public Task<DResult<TRow>> Exec<TRow>(DQuery<TRow> query) =>
                Connection.Exec(query.BuildQueryStatement(Table, Columns.ToArray()), query.ParseRow);

            public ExplorerContext Merge(ExplorerContext other)
            {
                if (!(other is CheckedContext checkedOther))
                {
                    throw new ArgumentException("Cannot merge two contexts of different concrete type.");
                }
                if (!ReferenceEquals(Connection, checkedOther.Connection))
                {
                    throw new ArgumentException("Cannot merge two contexts with different Connections.");
                }
                if (!string.Equals(DataSource, other.DataSource, StringComparison.Ordinal))
                {
                    throw new ArgumentException("Cannot merge two contexts with different DataSources.");
                }
                if (!string.Equals(Table, other.Table, StringComparison.Ordinal))
                {
                    throw new ArgumentException("Cannot merge two contexts with different Tables.");
                }

                return new CheckedContext(
                    Connection,
                    DataSource,
                    Table,
                    Columns.AddRange(other.Columns).Distinct(),
                    ColumnInfos.AddRange(other.ColumnInfos).Distinct());
            }
        }
    }
}
