namespace Explorer.Metrics
{
    using System.Text.Json.Serialization;

    public interface ExploreMetric
    {
        public string Name { get; }

        public object Metric { get; }

        [JsonIgnore]
        public int Priority { get; }
    }
}