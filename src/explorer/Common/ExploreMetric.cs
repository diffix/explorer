namespace Explorer.Common
{
    using System.Text.Json.Serialization;

    public interface ExploreMetric
    {
        public string Name { get; }

        [JsonPropertyName("value")]
        public object Metric { get; }

        [JsonIgnore]
        public int Priority { get; }

        public static TypedMetric<T> Create<T>(MetricDefinition<T> definition, T metric, int priority = 0)
        where T : notnull
        {
            return new TypedMetric<T>(definition, metric, priority);
        }
    }
}