namespace Diffix
{
    using System.Threading.Tasks;

    public interface DConnection
    {
        Task<DResult<TRow>> Exec<TRow>(DQuery<TRow> query);

        void Cancel();

        void ThrowIfCancellationRequested();
    }
}