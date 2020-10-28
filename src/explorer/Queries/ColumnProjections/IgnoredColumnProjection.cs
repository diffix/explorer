namespace Explorer.Queries
{
    using System;
    using System.Text.Json;

    public class IgnoredColumnProjection : ColumnProjection
    {
        public IgnoredColumnProjection(string column, int index)
        : base(column, index)
        {
        }

        public override string Project() => throw new InvalidOperationException("This column should be ignored.");

        public override object? Invert(JsonElement value) => null;
    }
}