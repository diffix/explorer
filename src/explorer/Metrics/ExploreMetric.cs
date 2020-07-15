namespace Explorer.Metrics
{
    public interface ExploreMetric
    {
        public string Name { get; }

        public object Metric { get; }

        public int Priority { get; }
    }
}