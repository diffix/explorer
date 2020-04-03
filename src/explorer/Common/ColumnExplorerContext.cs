namespace Explorer.Common
{
    using Diffix;

    internal class ColumnExplorerContext : ExplorerContext
    {
        public ColumnExplorerContext(string tableName, string columnName, DValueType columnType)
        {
            Table = tableName;
            Column = columnName;
            ColumnType = columnType;
        }

        public string Table { get; }

        public string Column { get; }

        public DValueType ColumnType { get; }
    }
}