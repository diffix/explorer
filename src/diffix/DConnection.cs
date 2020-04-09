namespace Diffix
{
    using System;
    using System.Threading.Tasks;

    public interface DConnection
    {
        bool IsCancellationRequested { get; }

        Task<DResult<TRow>> Exec<TRow>(DQuery<TRow> query);

        void Cancel();

        void ThrowIfCancellationRequested() => throw new OperationCanceledException("Query operation was cancelled.");
    }
}