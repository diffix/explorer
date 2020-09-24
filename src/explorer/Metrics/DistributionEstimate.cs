namespace Explorer.Metrics
{
    using System.Collections.Generic;
    using System.Collections.Immutable;

    public class DistributionEstimate
    {
        public DistributionEstimate(string name, string? distribution, IEnumerable<GoodnessMetric> goodness)
        {
            Name = name;
            Distribution = distribution;
            Goodness = ImmutableArray.CreateRange(goodness);
        }

        public string Name { get; }

        public string? Distribution { get; }

        public ImmutableArray<GoodnessMetric> Goodness { get; }
    }
}