namespace Explorer.Common
{
    using System.Collections.Generic;

    public static class MetricDefinitions
    {
        public static readonly MetricDefinition<long> Count = new MetricDefinition<long>("count");
        public static readonly MetricDefinition<decimal> AverageEstimate = new MetricDefinition<decimal>("average_estimate");
        public static readonly MetricDefinition<bool> IsCategorical = new MetricDefinition<bool>("is_categorical");
        public static readonly MetricDefinition<IEnumerable<object>> SampleValues = new MetricDefinition<IEnumerable<object>>("sample_values");

        public static MetricDefinition<T> Min<T>()
        where T : unmanaged
        {
            return new MetricDefinition<T>("min");
        }

        public static MetricDefinition<T> Max<T>()
        where T : unmanaged
        {
            return new MetricDefinition<T>("max");
        }
    }
}
