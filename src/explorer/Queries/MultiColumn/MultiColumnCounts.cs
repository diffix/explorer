namespace Explorer.Queries
{
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;
    using System.Text.Json;

    using Diffix;
    using Explorer.Common;

    public class MultiColumnCounts : DQuery<MultiColumnCounts.Result>
    {
        private const int DefaultMaxGroupSize = 2;

        public MultiColumnCounts(IEnumerable<ColumnProjection> projections)
        {
            ColumnProjections = projections.ToImmutableArray();
        }

        public int MaxGroupSize { get; set; } = DefaultMaxGroupSize;

        private ImmutableArray<ColumnProjection> ColumnProjections { get; }

        private ImmutableArray<string> Columns { get; set; } = ImmutableArray<string>.Empty;

        public override Result ParseRow(ref Utf8JsonReader reader) => new Result(
            ref reader, Columns);

        protected override string GetQueryStatement(string table, IEnumerable<string> quotedColumns)
        {
            Columns = ColumnProjections.Select(p => p.Column).ToImmutableArray();

            var columnsFragment = string.Join(",", ColumnProjections.Select(p => p.Project()));

            var groupingSets = Enumerable.Range(2, Columns.Length).EnumeratePermutations(MaxGroupSize);
            var groupIndicesFragment = string.Join(",", groupingSets.Select(g => $"({string.Join(",", g)})"));

            return $@"
                select
                    grouping_id(
                        {columnsFragment}
                    ),
                    {columnsFragment},
                    count(*),
                    count_noise(*)
                from {table}
                group by grouping sets ({groupIndicesFragment})";
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