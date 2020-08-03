namespace Explorer.Tests
{
    using System.Threading.Tasks;

    using Aircloak.JsonApi;
    using Diffix;

    public class ExplorerTestContext : ExplorerContext
    {
        public ExplorerTestContext(AircloakConnection connection, string dataSource, string table, string column, DColumnInfo columnInfo)
        {
            Connection = connection;
            DataSource = dataSource;
            Table = table;
            Column = column;
            ColumnInfo = columnInfo;
        }

        public AircloakConnection Connection { get; }

        public string DataSource { get; set; }

        public string Table { get; set; }

        public string Column { get; set; }

        public DColumnInfo ColumnInfo { get; set; }

        public Task<DResult<TRow>> Exec<TRow>(DQuery<TRow> query) =>
            Connection.Exec(query.BuildQueryStatement(Table, Column), query.ParseRow);
    }
}