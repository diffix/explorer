namespace Explorer
{
    using System;
    using Diffix;

    public sealed class ColumnExploration : AbstractExploration
    {
        public ColumnExploration(ExplorationScope scope)
        : base(scope)
        {
            if (!(Context is ExplorerContext))
            {
                throw new InvalidOperationException(
                    $"{nameof(ColumnExploration)} requires a context object in the {nameof(ExplorationScope)}!");
            }

            try
            {
                Column = Context.Column;
                ColumnInfo = Context.ColumnInfo;
            }
            catch (InvalidOperationException)
            {
                throw new InvalidOperationException(
                    $"{nameof(ColumnExploration)} requires a single-column context but context has {Context.Columns.Length} columns.");
            }
        }

        public string Column { get; }

        public DColumnInfo ColumnInfo { get; }
    }
}