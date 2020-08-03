namespace Explorer.Queries
{
    using System.Linq;
    using System.Text.Json;

    using Diffix;
    using Explorer.Common;

    internal class TextColumnSuffix :
        DQuery,
        DResultParser<ValueWithCount<string>>
    {
        private readonly int minLength;
        private readonly int maxLength;

        public TextColumnSuffix(int minLength, int maxLength)
        {
            this.minLength = minLength;
            this.maxLength = maxLength;
        }

        public ValueWithCount<string> ParseRow(ref Utf8JsonReader reader) =>
            new ValueWithCount<string>(ref reader);

        protected override string GetQueryStatement(string table, string column)
        {
            var indexes = Enumerable.Range(minLength, maxLength - minLength + 1);
            var columnNames = string.Join(", ", indexes.Select(i => $"s{i}"));
            var suffixExpressions = string.Join(",\n", indexes.Select(i => $"    right({column}, {i}) as s{i}"));

            return $@"
                select
                    concat({columnNames}) as suffix,
                    sum(count),
                    sum(count_noise)
                from (
                    select
                        {suffixExpressions},
                        count(*),
                        count_noise(*)
                    from {table}
                    group by grouping sets ({columnNames})
                    ) as suffixes
                group by suffix
                order by sum(count) desc";
        }
    }
}