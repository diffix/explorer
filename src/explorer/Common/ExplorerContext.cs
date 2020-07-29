namespace Explorer.Common
{
    using System.Threading.Tasks;

    using Diffix;

    public interface ExplorerContext
    {
        public DConnection Connection { get; }

        public string DataSource { get; }

        public DSqlObjectName Table { get; }

        public DSqlObjectName Column { get; }

        public DColumnInfo ColumnInfo { get; }

        public Task<DResult<TRow>> Exec<TRow>(DQuery<TRow> query) => Connection.Exec(query);
    }
}