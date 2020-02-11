namespace Aircloak.JsonApi
{
    public interface IQuerySpec<TRow> : IRowReader<TRow>
    {
        public string QueryStatement { get; }
    }
}