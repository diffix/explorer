namespace Explorer
{
    internal struct UntypedMetric : IExploreMetric
    {
        public UntypedMetric(string name, object metric)
        {
            Name = name;
            Metric = metric;
        }

        public string Name { get; }

        public object Metric { get; set; }
    }
}