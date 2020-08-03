namespace Explorer.Queries
{
    using System.Linq;
    using System.Text.Json;

    using Diffix;
    using Explorer.Common;
    using Explorer.JsonExtensions;

    internal class TextColumnSubstring : DQuery<TextColumnSubstring.Result>
    {
        private readonly int pos;
        private readonly int length;
        private readonly int count;

        public TextColumnSubstring(int pos, int length, int count)
        {
            this.pos = pos;
            this.length = length;
            this.count = count;
        }

        public string GetQueryStatement(string table, string column)
        {
            var indexes = Enumerable.Range(0, count);
            var columnNames = string.Join(", ", indexes.Select(i => $"s{i}"));
            var substringExpressions = string.Join(",\n                        ", indexes.Select(i =>
                $"substring({column}, {pos + i + 1}, {length}) as s{i}"));
            var whenExpressions = string.Join("\n                        ", indexes.Select(i =>
                $"when s{i} is not null then {i}"));

            return $@"
                select
                    concat({columnNames}) as sstr,
                    sum(count),
                    sum(count_noise),
                    case
                        {whenExpressions}
                    end as i
                from (
                    select
                        {substringExpressions},
                        count(*),
                        count_noise(*)
                    from {table}
                    group by grouping sets ({columnNames})
                    ) as substring_counts
                group by {columnNames}
                having length(sstr) = {length}";
        }

        public Result ParseRow(ref Utf8JsonReader reader) => new Result(ref reader);

        public class Result : ValueWithCount<string>
        {
            public Result(ref Utf8JsonReader reader)
                : base(ref reader)
            {
                var index = reader.ParseDValue<int>();
                Index = index.HasValue ? index.Value : -1;
            }

            public int Index { get; }
        }
    }
}