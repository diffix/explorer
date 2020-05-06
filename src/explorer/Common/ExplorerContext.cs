namespace Explorer.Common
{
    using Diffix;

    public interface ExplorerContext
    {
        public string Table { get; }

        public string Column { get; }

        public DValueType ColumnType { get; }
    }
}