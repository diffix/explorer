namespace Explorer.Common
{
    using Diffix;

    public interface ExplorerContext
    {
        public string DataSource { get; }

        public DSqlObjectName Table { get; }

        public DSqlObjectName Column { get; }

        public DColumnInfo ColumnInfo { get; }
    }
}