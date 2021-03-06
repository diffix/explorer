namespace Explorer.Components
{
    using System.Threading.Tasks;

    public interface ResultProvider<TResult>
    where TResult : class
    {
        Task<TResult?> ResultAsync { get; }
    }
}
