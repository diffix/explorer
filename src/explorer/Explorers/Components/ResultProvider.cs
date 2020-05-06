namespace Explorer.Explorers.Components
{
    using System.Threading.Tasks;

    internal interface ResultProvider<TResult>
    {
        Task<TResult> ResultAsync { get; }
    }
}
