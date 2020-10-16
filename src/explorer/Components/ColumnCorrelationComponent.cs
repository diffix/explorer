namespace Explorer.Components
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;
    using System.Threading.Tasks;

    using Explorer.Common;
    using Explorer.Components.ResultTypes;
    using Explorer.Metrics;
    using Explorer.Queries;

    public class ColumnCorrelationComponent :
        ExplorerComponent<ColumnCorrelationComponent.Result>,
        PublisherComponent
    {
        public const int DefaultMaxCorrelationDepth = 2;

        public ColumnCorrelationComponent(
            IEnumerable<ColumnProjection> projections)
        {
            Projections = projections.ToImmutableArray();
        }

        public int MaxCorrelationDepth { get; set; } = DefaultMaxCorrelationDepth;

        public ImmutableArray<ColumnProjection> Projections { get; }

        /// <summary>
        /// Drills down into succesive column combinations, combining the results.
        /// <para>
        /// For example: We have columns {A, B, C, D}
        /// First, this will get counts for {A}, {B}, {C}, {D}.
        /// then: {A, B}, {A, C}, {A, D} - ONLY if {A} returned no suppressed columns
        /// then: {B, A}, {B, C}, {B, D} - ONLY if {B} returned no suppressed columns
        /// etc...
        /// then: {A, B, C, D} - ONLY if {A, B, C} returned no suppressed columns
        /// then: {A, B, D, C} - ONLY if {A, B, D} returned no suppressed columns
        /// etc.
        /// </para>
        /// <para>
        /// Thus, it 'searches' the data sets, grouping by ever increasing column combinations, until it reaches a
        /// 'dead-end' where the combination is suppressed.
        /// </para>
        /// </summary>
        /// <param name="context">An <see cref"ExplorerContext" /> containing the query execution method.</param>
        /// <param name="projections">
        /// A list of <see cref="ColumnProjection" />s defining how to segment the columns into buckets.
        /// </param>
        /// <param name="maxLevel">
        /// The maximum number of columns to include in a subgrouping, or null for all columns.
        /// </param>
        /// <returns>A Task that resolves to a list of query result rows.</returns>
        public static async Task<IEnumerable<MultiColumnCounts.Result>> DrillDown(
            ExplorerContext context,
            IEnumerable<ColumnProjection> projections,
            int? maxLevel = null)
        {
            maxLevel ??= projections.Count();
            var numLevels = Math.Min(maxLevel.Value, projections.Count());
            var allLevels = new List<IEnumerable<MultiColumnCounts.Result>>(numLevels);

            var rootLevel = await context.Exec(new MultiColumnCountsPartial(projections));
            allLevels.Add(rootLevel.Rows);

            foreach (var level in Enumerable.Range(0, numLevels - 1))
            {
                var currentLevel = allLevels[level];
                var nextLevel = await DrillDownNextLevel(context, projections, currentLevel);
                if (!nextLevel.Any())
                {
                    break;
                }
                allLevels.Add(nextLevel.ToList());
            }

            return allLevels.Flatten();
        }

        /// <summary>
        /// Performs the drill-down query for a single level, starting from a given list of results as the root.
        /// <para>
        /// For example, if the columns are {A, B, C, D, E} and the root contains results for the groupings:
        /// <c>
        ///     [0, 2] / [A, C]
        ///     [1, 2] / [B, C]
        /// </c>
        /// then following combinations will be queried:
        /// <c>
        ///     [0, 2, 1] / [A, C, B]
        ///     [0, 2, 3] / [A, C, D]
        ///     [0, 2, 4] / [A, C, E]
        ///     [1, 2, 0] / [B, C, A]
        ///     [1, 2, 3] / [B, C, D]
        ///     [1, 2, 4] / [B, C, E]
        /// </c>
        /// Note that [0, 2, 1] and [1, 2, 0] are equivalent (same grouping_id). When this happens, only the grouping
        /// with the lower suppressed value count is retained.
        /// </para>
        /// </summary>
        /// <param name="context">An <see cref"ExplorerContext" /> containing the query execution method.</param>
        /// <param name="projections">
        /// A list of <see cref="ColumnProjection" />s defining how to segment the columns into buckets.
        /// </param>
        /// <param name="root">A list of results to be used as the root (see explanation above).</param>
        /// <returns>A list of results at the next level above the provided root.</returns>
        public static async Task<IEnumerable<MultiColumnCounts.Result>> DrillDownNextLevel(
            ExplorerContext context,
            IEnumerable<ColumnProjection> projections,
            IEnumerable<MultiColumnCounts.Result> root)
        {
            var nextLevel = new ConcurrentDictionary<ColumnGrouping, IEnumerable<MultiColumnCounts.Result>>();
            await Task.WhenAll(
                root.GroupBy(_ => _.ColumnGrouping).Select(async grouping =>
                {
                    if (grouping.All(row => row.IsSuppressed))
                    {
                        return;
                    }

                    var indices = grouping.Key.Indices;
                    var partialResult = await context.Exec(
                        new MultiColumnCountsPartial(projections, indices));

                    foreach (var newGrouping in partialResult.Rows.GroupBy(_ => _.ColumnGrouping))
                    {
                        nextLevel.AddOrUpdate(newGrouping.Key, newGrouping, (_, oldGrouping) =>
                        {
                            var oldSuppressedCount = oldGrouping.Aggregate(
                                0L, (acc, v) => acc + (v.IsSuppressed ? v.Count : 0));

                            var newSuppressedCount = newGrouping.Aggregate(
                                0L, (acc, v) => acc + (v.IsSuppressed ? v.Count : 0));

                            return newSuppressedCount < oldSuppressedCount
                                ? newGrouping
                                : oldGrouping;
                        });
                    }
                }));

            return nextLevel.Values.Flatten();
        }

        public async IAsyncEnumerable<ExploreMetric> YieldMetrics()
        {
            var correlationResult = await ResultAsync;
            if (correlationResult == null)
            {
                yield break;
            }

            yield return new UntypedMetric(
                name: "correlations",
                metric: correlationResult.Probabilities
                    .Select(kv => new Correlation(
                        kv.Key.Indices.Select(i => correlationResult.Projections[i].Column).ToArray(),
                        kv.Value.CorrelationFactor))
                    .ToList());
        }

        protected async override Task<Result?> Explore()
        {
            if (Projections.IsEmpty)
            {
                return null;
            }

            var queryResults = await DrillDown(Context, Projections, MaxCorrelationDepth);

            var groups = queryResults.GroupBy(row => row.ColumnGrouping);

            var groupingIdConverter = GroupingIdConverter.GetConverter(Projections.Length);

            var cardinalities = groups
                .Where(grouping =>
                    grouping.Key.Indices.Length == 1) // single-column aggregates
                .ToDictionary(
                    _ => _.Key.Indices.Single(), // the index of the column
                    _ => _.Count()); // number of categories/buckets/whatever

            var matrices = groups
                .Select(grouping =>
                {
                    var groupingIndices = grouping.Key.Indices;

                    var matrix = new JointProbabilityMatrix(groupingIndices.Select(i => cardinalities[i]));

                    foreach (var bucket in grouping)
                    {
                        matrix.AddBucket(bucket);
                    }

                    return (grouping.Key, matrix);
                });

            return new Result(matrices, Projections);
        }

        public class Result
        {
            internal Result(
                IEnumerable<(ColumnGrouping groupingIndices, JointProbabilityMatrix matrix)> matrices,
                ImmutableArray<ColumnProjection> projections)
            {
                Probabilities = new Dictionary<ColumnGrouping, JointProbabilityMatrix>(
                    matrices.Select(c => KeyValuePair.Create(c.groupingIndices, c.matrix)));

                Projections = projections;
            }

            public Dictionary<ColumnGrouping, JointProbabilityMatrix> Probabilities { get; }

            public ImmutableArray<ColumnProjection> Projections { get; }
        }
    }
}