namespace Explorer.Common
{
    using Diffix;

    public interface ExplorerContext
    {
        public string Table { get; }

        public string Column { get; }

        public DValueType ColumnType { get; }
    }

    public class RawExplorerContext : ExplorerContext
    {
#nullable disable
        public RawExplorerContext()
        {
        }
#nullable enable

        public RawExplorerContext(ExplorerContext ctx)
        {
            Table = ctx.Table;
            Column = ctx.Column;
            ColumnType = ctx.ColumnType;
        }

        public string Table { get; set; }

        public string Column { get; set; }

        public DValueType ColumnType { get; set; }
    }
}