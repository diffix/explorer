namespace Explorer.Tests
{
    using Diffix;
    using Explorer.Common;

    public class ExplorerTestContext : ExplorerContext
    {
        public ExplorerTestContext(DConnection connection, string dataSource, string table, string column, DColumnInfo columnInfo)
        {
            Connection = connection;
            DataSource = dataSource;
            Table = new DSqlObjectName(table);
            Column = new DSqlObjectName(column);
            ColumnInfo = columnInfo;
        }

        public ExplorerTestContext(ExplorerContext ctx)
        {
            Connection = ctx.Connection;
            DataSource = ctx.DataSource;
            Table = ctx.Table;
            Column = ctx.Column;
            ColumnInfo = ctx.ColumnInfo;
        }

        public DConnection Connection { get; }

        public string DataSource { get; set; }

        public DSqlObjectName Table { get; set; }

        public DSqlObjectName Column { get; set; }

        public DColumnInfo ColumnInfo { get; set; }
    }
}