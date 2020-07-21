namespace Explorer.Tests
{
    using Diffix;
    using Explorer.Common;

    public class ExplorerTestContext : ExplorerContext
    {
        public ExplorerTestContext()
        {
        }

        public ExplorerTestContext(ExplorerContext ctx)
        {
            DataSource = ctx.DataSource;
            Table = ctx.Table;
            Column = ctx.Column;
            ColumnType = ctx.ColumnType;
        }

        public string DataSource { get; set; } = string.Empty;

        public DSqlObjectName Table { get; set; } = new DSqlObjectName(string.Empty);

        public DSqlObjectName Column { get; set; } = new DSqlObjectName(string.Empty);

        public DValueType ColumnType { get; set; } = DValueType.Unknown;
    }
}