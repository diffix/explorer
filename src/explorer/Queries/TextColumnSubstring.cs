namespace Explorer.Queries
{
    using System.Linq;
    using System.Text.Json;

    using Diffix;
    using Explorer.Common;
    using Explorer.JsonExtensions;

    internal class TextColumnSubstring : DQuery<TextColumnSubstring.Result>
    {
        public TextColumnSubstring(string tableName, string columnName, int pos, int length, int count)
        {
            var indexes = Enumerable.Range(0, count);
            var columnNames = string.Join(", ", indexes.Select(i => $"s{i}"));
            var substringExpressions = string.Join(",\n                        ", indexes.Select(i =>
                $"substring({columnName}, {pos + i + 1}, {length}) as s{i}"));
            var whenExpressions = string.Join("\n                        ", indexes.Select(i =>
                $"when s{i} is not null then {i}"));

            QueryStatement = $@"
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
                    from {tableName}
                    group by grouping sets ({columnNames})
                    ) as substring_counts 
                group by {columnNames}
                having length(sstr) = {length}";
        }

        public string QueryStatement { get; }

        public Result ParseRow(ref Utf8JsonReader reader)
        {
            var value = reader.ParseDValue<string>();
            var count = reader.ParseCount();
            var countNoise = reader.ParseCountNoise();
            var index = reader.ParseDValue<int>();
            return new Result(value, count, countNoise, index.HasValue ? index.Value : -1);
        }

        public class Result : ValueWithCount<string>
        {
            public Result(DValue<string> value, long count, double? countNoise, int index)
                : base(value, count, countNoise)
            {
                Index = index;
            }

            public int Index { get; }
        }
    }
}