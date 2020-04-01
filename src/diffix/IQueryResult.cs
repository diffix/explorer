namespace Diffix
{
    using System.Collections.Generic;

    public interface IQueryResult<TRow>
    {
        IEnumerable<TRow> ResultRows { get; }
    }
}