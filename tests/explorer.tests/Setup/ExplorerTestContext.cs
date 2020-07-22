namespace Explorer.Tests
{
    using Diffix;
    using Explorer.Common;

    public class ExplorerTestContext : ExplorerContext
    {
        public ExplorerTestContext()
        {
            DataSource = string.Empty;
            Table = string.Empty;
            Column = string.Empty;
            ColumnInfo = new DColumnInfo(DValueType.Unknown, false, true);
        }

        public ExplorerTestContext(ExplorerContext ctx)
        {
            DataSource = ctx.DataSource;
            Table = ctx.Table;
            Column = ctx.Column;
            ColumnInfo = ctx.ColumnInfo;
        }

        public string DataSource { get; set; }

        public string Table { get; set; }

        public string Column { get; set; }

        public DColumnInfo ColumnInfo { get; set; }
    }
}