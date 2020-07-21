namespace Explorer.Tests
{
    using Diffix;
    using Explorer.Common;

    public class ExplorerTestContext : ExplorerContext
    {
        public ExplorerTestContext(string dataSource, string table, string column, DValueType columnType)
        {
            DataSource = dataSource;
            Table = Quote(table);
            Column = Quote(column);
            ColumnType = columnType;
        }

        public ExplorerTestContext(ExplorerContext ctx)
        {
            DataSource = ctx.DataSource;
            Table = Quote(ctx.Table);
            Column = Quote(ctx.Column);
            ColumnType = ctx.ColumnType;
        }

        public string DataSource { get; }

        public string Table { get; }

        public string Column { get; }

        public DValueType ColumnType { get; }

        private static string Quote(string name) => "\"" + name + "\"";
    }
}