namespace Explorer.Explorers.Components
{
    using System.Threading.Tasks;

    public interface ResultProvider<TResult>
    {
        Task<TResult> ResultAsync { get; }
    }
}
