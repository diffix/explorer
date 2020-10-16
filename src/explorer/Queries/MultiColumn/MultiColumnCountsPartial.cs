namespace Explorer.Queries
{
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;
    using System.Text.Json;

    using Diffix;

    public class MultiColumnCountsPartial : DQuery<MultiColumnCounts.Result>
    {
        public MultiColumnCountsPartial(
            IEnumerable<ColumnProjection> projections,
            IEnumerable<int>? rootIndices = default)
        {
            ColumnProjections = projections.ToImmutableArray();
            RootIndices = rootIndices?.ToImmutableArray() ?? ImmutableArray<int>.Empty;
        }

        public ImmutableArray<int> RootIndices { get; }

        private ImmutableArray<ColumnProjection> ColumnProjections { get; }

        private ImmutableArray<string> Columns { get; set; } = ImmutableArray<string>.Empty;

        public override MultiColumnCounts.Result ParseRow(ref Utf8JsonReader reader) => new MultiColumnCounts.Result(
            ref reader, Columns);

        protected override string GetQueryStatement(string table, IEnumerable<string> quotedColumns)
        {
            const int indexOffset = 2;

            Columns = ColumnProjections.Select(p => p.Column).ToImmutableArray();

            var columnsFragment = string.Join(",", ColumnProjections.Select(p => p.Project()));

            var groupingSets = Enumerable.Range(0, Columns.Length)
                .Except(RootIndices)
                .Select(RootIndices.Append)
                .Select(g => g.Select(i => i + indexOffset));

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
    }
}