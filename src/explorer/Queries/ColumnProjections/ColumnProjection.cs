namespace Explorer.Queries
{
    using System.Text.Json;

    public abstract class ColumnProjection
    {
        protected ColumnProjection(string column, int sourceIndex)
        {
            Column = column;
            SourceIndex = sourceIndex;
        }

        public string Column { get; }

        public int SourceIndex { get; }

        public abstract string Project();

        public abstract object? Invert(JsonElement value);
    }
}