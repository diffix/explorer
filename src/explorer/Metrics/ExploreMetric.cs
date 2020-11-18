namespace Explorer.Metrics
{
    using System.Text.Json.Serialization;

    public interface ExploreMetric
    {
        public string Name { get; }

        [JsonPropertyName("value")]
        public object Metric { get; }

        public int Priority { get; }

        public bool Invisible { get; }
    }
}