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
            ApiUrl = ctx.ApiUrl;
            DataSource = ctx.DataSource;
            Table = ctx.Table;
            Column = ctx.Column;
            ColumnType = ctx.ColumnType;
        }

        public System.Uri ApiUrl { get; set; } = new System.Uri(string.Empty);

        public string DataSource { get; set; } = string.Empty;

        public string Table { get; set; } = string.Empty;

        public string Column { get; set; } = string.Empty;

        public DValueType ColumnType { get; set; } = DValueType.Unknown;
    }
}