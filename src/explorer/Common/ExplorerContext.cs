namespace Explorer.Common
{
    using Diffix;

    internal class ExplorerContext
    {
        public ExplorerContext(string tableName, string columnName, DValueType columnType)
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