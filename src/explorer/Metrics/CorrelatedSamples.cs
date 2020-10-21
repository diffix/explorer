namespace Explorer.Metrics
{
    using System.Collections.Generic;
    using System.Linq;
    using Explorer.Queries;

    public class CorrelatedSamples
    {
        public static readonly string MetricName = "correlated_samples";

        public CorrelatedSamples(IEnumerable<ColumnProjection> projections, List<List<object?>> samples)
        {
            SourceIndices = projections.Select(p => p.SourceIndex).ToList();
            Columns = projections.Select(p => p.Column).ToList();
            Samples = samples;
        }

        public List<List<object?>> Samples { get; }

        public List<int> SourceIndices { get; }

        public List<string> Columns { get; }

        public IEnumerable<IEnumerable<(int, object?)>> ByIndex
        {
            get
            {
                foreach (var row in Samples)
                {
                    yield return SourceIndices.Zip(row);
                }
            }
        }

        public IEnumerable<IEnumerable<(string, object?)>> ByColumn
        {
            get
            {
                foreach (var row in Samples)
                {
                    yield return Columns.Zip(row);
                }
            }
        }

        public ExploreMetric AsMetric() => new UntypedMetric(MetricName, this);
    }
}