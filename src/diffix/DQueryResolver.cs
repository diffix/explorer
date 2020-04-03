namespace Diffix
{
    using System.Threading.Tasks;

    public interface DQueryResolver
    {
        Task<DResult<TRow>> Resolve<TRow>(DQuery<TRow> query);

        void Cancel();

        void ThrowIfCancellationRequested();
    }
}