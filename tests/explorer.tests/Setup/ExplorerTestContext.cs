namespace Explorer.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;
    using System.Threading.Tasks;

    using Aircloak.JsonApi;
    using Diffix;

    public class ExplorerTestContext : ExplorerContext
    {
        public ExplorerTestContext(
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

        public ExplorerTestContext(
            AircloakConnection connection,
            string dataSource,
            string table,
            IEnumerable<string> columns,
            IEnumerable<DColumnInfo> columnInfo)
        {
            Connection = connection;
            DataSource = dataSource;
            Table = table;
            Columns = ImmutableArray.CreateRange(columns);
            ColumnInfos = ImmutableArray.CreateRange(columnInfo);
        }

        public AircloakConnection Connection { get; }

        public string DataSource { get; set; }

        public string Table { get; set; }

        public ImmutableArray<string> Columns { get; }

        public ImmutableArray<DColumnInfo> ColumnInfos { get; }

        public Task<DResult<TRow>> Exec<TQuery, TRow>(TQuery query)
        where TQuery : DQueryStatement, DResultParser<TRow>
        {
            return Connection.Exec(query.BuildQueryStatement(Table, Columns.ToArray()), query.ParseRow);
        }

        public Task<DResult<TRow>> Exec<TRow>(DQuery<TRow> query)
        {
            return Connection.Exec(query.BuildQueryStatement(Table, Columns.ToArray()), query.ParseRow);
        }

        public ExplorerContext Merge(ExplorerContext other)
        {
            if (!(other is ExplorerTestContext checkedOther))
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

            return new ExplorerTestContext(
                Connection,
                DataSource,
                Table,
                Columns.AddRange(other.Columns).Distinct(),
                ColumnInfos.AddRange(other.ColumnInfos).Distinct());
        }
    }
}