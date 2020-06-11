namespace Explorer.Metrics
{
    public struct UntypedMetric : ExploreMetric
    {
        internal UntypedMetric(string name, object metric)
        {
            Name = name;
            Metric = metric;
        }

        public string Name { get; }

        public object Metric { get; }
    }
}