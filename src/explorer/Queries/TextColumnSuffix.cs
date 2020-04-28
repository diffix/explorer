namespace Explorer.Queries
{
    using System.Linq;
    using System.Text.Json;

    using Diffix;
    using Explorer.Common;

    internal class TextColumnSuffix :
        DQuery<ValueWithCount<string>>
    {
        public TextColumnSuffix(string tableName, string columnName, int minLength, int maxLength)
        {
            // TODO: determine suffix length dynamically
            var indexes = Enumerable.Range(minLength, maxLength - minLength + 1);
            var columnNames = string.Join(", ", indexes.Select(i => $"s{i}"));
            var suffixExpressions = string.Join(",\n", indexes.Select(i => $"    right({columnName}, {i}) as s{i}"));

            QueryStatement = $@"
                select 
                    concat({columnNames}) as suffix, 
                    sum(count), 
                    sum(count_noise)
                from (
                    select 
                        {suffixExpressions},
                        count(*),
                        count_noise(*)
                    from {tableName}
                    group by grouping sets ({columnNames})
                    ) as suffixes
                group by suffix
                order by sum(count) desc";
        }

        public string QueryStatement { get; }

        public ValueWithCount<string> ParseRow(ref Utf8JsonReader reader) =>
            new ValueWithCount<string>(ref reader);
    }
}