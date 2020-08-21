namespace Explorer.Queries
{
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;
    using System.Text.Json;

    using Diffix;
    using Explorer.Common;

    public class ColumnPairs : DQuery<ColumnPairs.Result>
    {
        private ImmutableArray<string> Columns { get; set; } = ImmutableArray<string>.Empty;

        public override Result ParseRow(ref Utf8JsonReader reader) => new Result(
            ref reader, Columns);

        protected override string GetQueryStatement(string table, IEnumerable<string> quotedColumns)
        {
            // Unquote the columns.
            Columns = quotedColumns.Select(c => c[1..^1]).ToImmutableArray();

            var columnsFragment = string.Join(",", quotedColumns);
            var groups = Enumerable.Range(3, quotedColumns.Count() - 1).Select(i => $"(2, {i})");

            return $@"
                select
                    grouping_id(
                        {columnsFragment}
                    ),
                    {columnsFragment},
                    count(*),
                    count_noise(*)
                from {table}
                group by grouping sets ({string.Join(",", groups)})";
        }

        public class Result : IndexedGroupingSetsResultMulti<string>
        {
            internal Result(ref Utf8JsonReader reader, IEnumerable<string> groupingLabels)
            : base(ref reader, groupingLabels.ToImmutableArray())
            {
            }
        }
    }
}