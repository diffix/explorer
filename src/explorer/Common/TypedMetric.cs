namespace Explorer.Common
{
    using System.Text.Json.Serialization;

    public sealed class TypedMetric<T> : ExploreMetric<T>
    where T : notnull
    {
        public TypedMetric(MetricDefinition<T> metricDefinition, T metric, int priority = 0)
        {
            Name = metricDefinition.Name;
            TMetric = metric;
            Priority = priority;
        }

        public string Name { get; }

        [JsonPropertyName("value")]
        public object Metric { get => TMetric; }

        [JsonIgnore]
        public T TMetric { get; }

        [JsonIgnore]
        public int Priority { get; }
    }
}