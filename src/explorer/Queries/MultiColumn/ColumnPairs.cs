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
        public ColumnPairs(IEnumerable<string> correlationColumns)
        {
            PairColumns = ImmutableArray.CreateRange(correlationColumns);
        }

        private ImmutableArray<string> PairColumns { get; set; } = ImmutableArray<string>.Empty;

        private string? BaseColumn { get; set; }

        public override Result ParseRow(ref Utf8JsonReader reader) => new Result(
            ref reader, PairColumns.Prepend(BaseColumn!));

        protected override string GetQueryStatement(string table, string baseColumn)
        {
            BaseColumn = baseColumn[1..^1];
            var pairFragment = string.Join(",", PairColumns.Select(Quote));
            var groups = Enumerable.Range(3, PairColumns.Length).Select(i => $"(2, {i})");

            return $@"
                select
                    grouping_id(
                        {baseColumn},
                        {pairFragment}
                    ),
                    {baseColumn},
                    {pairFragment},
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