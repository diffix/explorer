namespace Explorer.Common
{
    using System.Threading.Tasks;

    using Diffix;

    public interface ExplorerContext
    {
        public string DataSource { get; }

        public string Table { get; }

        public string Column { get; }

        public DColumnInfo ColumnInfo { get; }

        public Task<DResult<TRow>> Exec<TRow>(DQuery<TRow> query);
    }
}