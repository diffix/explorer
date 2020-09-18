namespace Explorer.Common
{
    public class MetricDefinition<T>
    where T : notnull
    {
        public MetricDefinition(string name) => Name = name;

        public string Name { get; }
    }
}