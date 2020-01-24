namespace Explorer
{
    using Aircloak.JsonApi;

    internal interface IQuerySpec<TRow>
        where TRow : IJsonArrayConvertible
    {
        public string QueryStatement { get; }
    }
}
