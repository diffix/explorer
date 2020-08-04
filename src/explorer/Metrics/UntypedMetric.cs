#pragma warning disable CA1815 // Struct type should override Equals

namespace Explorer.Metrics
{
    using System.Text.Json.Serialization;

    public struct UntypedMetric : ExploreMetric
    {
        public UntypedMetric(string name, object metric, int priority = 0)
        {
            Name = name;
            Metric = metric;
            Priority = priority;
        }

        public string Name { get; }

        public object Metric { get; }

        [JsonIgnore]
        public int Priority { get; }
    }
}

#pragma warning restore CA1815 // Struct type should override Equals