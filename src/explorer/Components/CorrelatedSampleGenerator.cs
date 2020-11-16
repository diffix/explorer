namespace Explorer.Components
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.Json;
    using Explorer.Common;
    using Explorer.Metrics;

    public class CorrelatedSampleGenerator : PublisherComponent
    {
        private static readonly JsonElement JsonNull = Utilities.MakeJsonNull();
        private readonly ResultProvider<ColumnCorrelationComponent.Result> correlationProvider;
        private readonly ExplorerContext context;

        public CorrelatedSampleGenerator(
            ResultProvider<ColumnCorrelationComponent.Result> correlationProvider,
            ExplorerContext context)
        {
            this.context = context;
            this.correlationProvider = correlationProvider;
            this.context = context;
        }

        private int SamplesToPublish => context.SamplesToPublish;

        public async IAsyncEnumerable<ExploreMetric> YieldMetrics()
        {
            var correlationResult = await correlationProvider.ResultAsync;
            if (correlationResult == null)
            {
                yield break;
            }

            // Add combinations greedily until we have joint probs for all columns
            // TODO:
            //   - improve on greedy algorithm. Eg. Optimize for highest sum of correlations.
            //   - decide how to include single-column probabilities (what is the correlation value?).
            var numColumns = correlationResult.Projections.Length;
            var includedColumns = new List<int>(numColumns);
            var samplePredictionSet = correlationResult
                .Probabilities
                .OrderByDescending(p => p.Value.CorrelationFactor)
                .Where(candidate =>
                {
                    // If we are within 1 column of the total, break since there are no single-column correlations.
                    // TODO: indlude single-column values (need to decide what correlation value to give them).
                    // if (includedColumns.Count >= numColumns - 1)
                    // {
                    //     return false;
                    // }

                    // Only consider a candidate if its columns are not already included.
                    if (!DoSetsOverlap(includedColumns, candidate.Key.Indices))
                    {
                        includedColumns.AddRange(candidate.Key.Indices);
                        return true;
                    }

                    return false;
                })
                .ToList();

            yield return new UntypedMetric(
                name: "sampled_correlations",
                metric: samplePredictionSet
                            .Select(kv => new Correlation(
                                kv.Key.Indices.Select(i => correlationResult.Projections[i].Column).ToArray(),
                                kv.Value.CorrelationFactor))
                            .OrderByDescending(_ => _.CorrelationFactor)
                            .ToList());

            // Generate samples based on the joint probabilities
            var samples = new List<List<object?>>(SamplesToPublish);

            for (var s = 0; s < SamplesToPublish; s++)
            {
                var sampleRow = new List<object?>(Enumerable.Repeat<object?>(null, numColumns));
                foreach (var (columnGrouping, probMatrix) in samplePredictionSet)
                {
                    var projections = columnGrouping.Indices.Select(i => correlationResult.Projections[i]);
                    var bucketValues = probMatrix.GetSample().ToList();

                    // If the returned sample contains no values, it was a suppressed bucket. Instead of returning
                    // nothing, generate a sample from each of the individual uncorrelated buckets.
                    if (bucketValues.Count == 0)
                    {
                        foreach (var grouping in columnGrouping.SingleColumnSubGroupings())
                        {
                            if (correlationResult.Probabilities.TryGetValue(grouping, out var probabilityMatrix))
                            {
                                var sample = probabilityMatrix.GetSampleUnsuppressed();
                                bucketValues.Add(sample.Any() ? sample.Single() : JsonNull);
                            }
                        }
                    }

                    foreach (var (i, value, projection) in columnGrouping.Indices.Zip2(bucketValues, projections))
                    {
                        sampleRow[i] = projection.Invert(value);
                    }
                }
                samples.Add(sampleRow);
            }

            yield return new CorrelatedSamples(correlationResult.Projections, samples).AsMetric();

            static bool DoSetsOverlap<T>(IEnumerable<T> left, IEnumerable<T> right)
                => left.Union(right).Count() < left.Count() + right.Count();
        }
    }
}