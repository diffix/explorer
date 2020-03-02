namespace Explorer
{
    internal interface IExploreMetric
    {
        public string Name { get; }

        public object Metric { get; set; }
    }
}