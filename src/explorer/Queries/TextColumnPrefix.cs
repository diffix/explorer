namespace Explorer.Queries
{
    using System.Text.Json;

    using Diffix;
    using Explorer.Common;

    internal class TextColumnPrefix :
        DQuery<ValueWithCount<string>>
    {
        public TextColumnPrefix(string tableName, string columnName, int length)
        {
            // TODO: determine prefix length dynamically
            QueryStatement = $@"
                select 
                    left({columnName}, {length}),
                    count(*),
                    count_noise(*)
                from {tableName}
                group by 1
                having length(left({columnName}, {length})) = {length}";
        }

        public string QueryStatement { get; }

        public ValueWithCount<string> ParseRow(ref Utf8JsonReader reader) =>
            ValueWithCount<string>.Parse(ref reader);
    }
}