namespace Explorer
{
    using System.Collections.Immutable;
    using System.Linq;
    using System.Threading.Tasks;

    using Diffix;

    public interface ExplorerContext
    {
        public string DataSource { get; }

        public string Table { get; }

        public virtual string Column { get => Columns.Single(); }

        public virtual DColumnInfo ColumnInfo { get => ColumnInfos.Single(); }

        public ImmutableArray<string> Columns { get; }

        public ImmutableArray<DColumnInfo> ColumnInfos { get; }

        public Task<DResult<TRow>> Exec<TQuery, TRow>(TQuery query)
            where TQuery : DQueryStatement, DResultParser<TRow>;

        public Task<DResult<TRow>> Exec<TRow>(DQuery<TRow> query);
    }
}