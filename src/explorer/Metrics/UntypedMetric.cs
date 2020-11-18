namespace Explorer.Metrics
{
    using System.Text.Json.Serialization;

    [JsonConverter(typeof(ExploreMetricConverter))]
    public class UntypedMetric : ExploreMetric
    {
        public UntypedMetric(string name, object metric, int priority = 0, bool invisible = false)
        {
            Name = name;
            Metric = metric;
            Priority = priority;
            Invisible = invisible;
        }

        public string Name { get; }

        public object Metric { get; }

        [JsonIgnore]
        public int Priority { get; }

        [JsonIgnore]
        public bool Invisible { get; }
    }
}