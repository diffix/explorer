namespace Explorer.Common
{
    public sealed class TypedMetric<T> : ExploreMetric
    where T : notnull
    {
        public TypedMetric(MetricDefinition<T> metricDefinition, T metric, int priority = 0)
        {
            Name = metricDefinition.Name;
            Metric = metric;
            Priority = priority;
        }

        public string Name { get; }

        public object Metric { get; }

        public int Priority { get; }
    }
}