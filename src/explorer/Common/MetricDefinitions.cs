namespace Explorer.Common
{
    using System.Collections.Generic;

    using Explorer.Metrics;

    public static class MetricDefinitions
    {
        public static readonly MetricDefinition<IEnumerable<object>> SampleValues = new MetricDefinition<IEnumerable<object>>("sample_values");
        public static readonly MetricDefinition<bool> IsCategorical = new MetricDefinition<bool>("is_categorical");
        public static readonly MetricDefinition<decimal> Min = new MetricDefinition<decimal>("min");
        public static readonly MetricDefinition<decimal> Max = new MetricDefinition<decimal>("max");
        public static readonly MetricDefinition<decimal> AverageEstimate = new MetricDefinition<decimal>("average_estimate");
        public static readonly MetricDefinition<NumericDistribution> NumericDescriptiveStats = new MetricDefinition<NumericDistribution>("descriptive_stats");
        public static readonly MetricDefinition<DatetimeDistribution> DateTimeDescriptiveStats = new MetricDefinition<DatetimeDistribution>("descriptive_stats");

        public static readonly MetricDefinition<long> SimpleStatsCount = new MetricDefinition<long>("count");

        public static MetricDefinition<T> SimpleStatsMin<T>()
        where T : unmanaged
        {
            return new MetricDefinition<T>("min");
        }

        public static MetricDefinition<T> SimpleStatsMax<T>()
        where T : unmanaged
        {
            return new MetricDefinition<T>("max");
        }
    }
}
