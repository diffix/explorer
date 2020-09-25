namespace Explorer.Common
{
    using System.Collections.Generic;

    using Explorer.Metrics;

    public static class MetricDefinitions
    {
        public static readonly MetricDefinition<ExplorationInfo> ExplorationInfo = new MetricDefinition<ExplorationInfo>("exploration_info");
        public static readonly MetricDefinition<IList<object>> SampleValues = new MetricDefinition<IList<object>>("sample_values");
        public static readonly MetricDefinition<bool> IsCategorical = new MetricDefinition<bool>("is_categorical");
        public static readonly MetricDefinition<decimal> Min = new MetricDefinition<decimal>("min");
        public static readonly MetricDefinition<decimal> Max = new MetricDefinition<decimal>("max");
        public static readonly MetricDefinition<long> SimpleStatsCount = new MetricDefinition<long>("count");
        public static readonly MetricDefinition<decimal> AverageEstimate = new MetricDefinition<decimal>("average_estimate");
        public static readonly MetricDefinition<IList<double>> QuartileEstimates = new MetricDefinition<IList<double>>("quartile_estimates");
        public static readonly MetricDefinition<IList<DistributionEstimate>> DistributionEstimates = new MetricDefinition<IList<DistributionEstimate>>("distribution_estimates");
        public static readonly MetricDefinition<NumericDistribution> NumericDescriptiveStats = new MetricDefinition<NumericDistribution>("descriptive_stats");
        public static readonly MetricDefinition<DatetimeDistribution> DateTimeDescriptiveStats = new MetricDefinition<DatetimeDistribution>("descriptive_stats");
        public static readonly MetricDefinition<Histogram> Histogram = new MetricDefinition<Histogram>("histogram");
        public static readonly MetricDefinition<CategoricalData> CategoricalData = new MetricDefinition<CategoricalData>("categorical_data");
        public static readonly MetricDefinition<TextFormat> TextFormat = new MetricDefinition<TextFormat>("text_format");
        public static readonly MetricDefinition<TextLengthDistribution> TextLengthDistribution = new MetricDefinition<TextLengthDistribution>("text_length_distribution");
        public static readonly MetricDefinition<ValueCounts> TextLengthCounts = new MetricDefinition<ValueCounts>("text_length_counts");

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
