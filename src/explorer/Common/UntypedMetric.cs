namespace Explorer.Common
{
    public class UntypedMetric : ExploreMetric
    {
        public UntypedMetric(string name, object metric, int priority = 0)
        {
            Name = name;
            Metric = metric;
            Priority = priority;
        }

        public string Name { get; }

        public object Metric { get; }

        public int Priority { get; }
    }
}