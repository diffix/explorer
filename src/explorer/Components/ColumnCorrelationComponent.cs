namespace Explorer.Components
{
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
        public ColumnCorrelationComponent(
            ExplorerContext context,
            IEnumerable<ColumnProjection> projections)
        {
            Context = context;
            Projections = projections.ToImmutableArray();
        }

        public ImmutableArray<ColumnProjection> Projections { get; }

        public async IAsyncEnumerable<ExploreMetric> YieldMetrics()
        {
            var correlationResult = await ResultAsync;
            if (correlationResult == null)
            {
                yield break;
            }

            yield return new UntypedMetric(
                name: "correlation_factors",
                metric: correlationResult.Probabilities
                    .Select(kv => new
                    {
                        Column = kv.Key.Select(i => correlationResult.Projections[i].Column).ToArray(),
                        kv.Value.CorrelationFactor,
                    })
                    .ToList());
        }

        protected async override Task<Result?> Explore()
        {
            var multiColumnCounts = await Context.Exec(new MultiColumnCounts(Projections));

            var groups = multiColumnCounts.Rows.GroupBy(row => row.GroupingId);

            var groupingIdConverter = GroupingIdConverter.GetConverter(Projections.Length);

            var singleColumnCounts = groups
                .Where(grouping =>
                    groupingIdConverter.SubGroupSize(grouping.Key) == 1) // single-column aggregates
                .ToDictionary(
                    _ => groupingIdConverter.SingleIndexFromGroupingId(_.Key), // the index of the column
                    _ => _.Count()); // number of categories/buckets/whatever

            var matrices = groups
                .Where(grouping =>
                    groupingIdConverter.SubGroupSize(grouping.Key) > 1)
                .Select(grouping =>
                {
                    var groupingIndices = groupingIdConverter.IndicesFromGroupingId(grouping.Key).ToArray();

                    var cardinalities = groupingIndices.Select(i => singleColumnCounts[i]).ToArray();

                    var matrix = new JointProbabilityMatrix(
                        groupingIndices.Select(i => Context.Columns[i]),
                        cardinalities);

                    foreach (var bucket in grouping)
                    {
                        matrix.AddBucket(bucket);
                    }

                    return (groupingIndices, matrix);
                });

            return new Result(matrices, Projections);
        }

        public class Result
        {
            internal Result(
                IEnumerable<(int[] groupingIndices, JointProbabilityMatrix matrix)> matrices,
                ImmutableArray<ColumnProjection> projections)
            {
                Probabilities = new Dictionary<int[], JointProbabilityMatrix>(
                    matrices.Select(c => KeyValuePair.Create(c.groupingIndices, c.matrix)));

                Projections = projections;
            }

            public Dictionary<int[], JointProbabilityMatrix> Probabilities { get; }

            public ImmutableArray<ColumnProjection> Projections { get; }
        }
    }
}