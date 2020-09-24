namespace Explorer.Metrics
{
    using Diffix;

    public class ExplorationInfo
    {
        public ExplorationInfo(string dataSource, string table, string column, DValueType columnType)
        {
            DataSource = dataSource;
            Table = table;
            Column = column;
            ColumnType = columnType;
        }

        public string DataSource { get; }

        public string Table { get; }

        public string Column { get; }

        public DValueType ColumnType { get; }
    }
}