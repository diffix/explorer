namespace Explorer.Common
{
    using Diffix;

    public interface ExplorerContext
    {
        public string DataSource { get; }

        public string Table { get; }

        public string Column { get; }

        public ColumnInfo ColumnInfo { get; }
    }
}