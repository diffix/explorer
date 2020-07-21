namespace Explorer.Queries
{
    using System.Text.Json;

    using Diffix;
    using Explorer.Common;

    internal class TextColumnPrefix :
        DQuery<ValueWithCount<string>>
    {
        public TextColumnPrefix(DSqlObjectName tableName, DSqlObjectName columnName, int length)
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
            new ValueWithCount<string>(ref reader);
    }
}