namespace Diffix
{
    using System.Collections.Generic;

    public interface DResult<TRow>
    {
        IEnumerable<TRow> Rows { get; }
    }
}