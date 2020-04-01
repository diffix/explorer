namespace Explorer
{
    public interface IExploreMetric
    {
        public string Name { get; set; }

        public object Metric { get; set; }
    }
}