namespace Explorer.Common
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json;

    using Explorer.Metrics;

    public static class MetricDefinitions
    {
        public static readonly MetricDefinition<ExplorationInfo> ExplorationInfo = new MetricDefinition<ExplorationInfo>("exploration_info");
        public static readonly MetricDefinition<IList<JsonElement>> SampleValues = new MetricDefinition<IList<JsonElement>>("sample_values");
        public static readonly MetricDefinition<IList<double>> SampleValuesDouble = new MetricDefinition<IList<double>>("sample_values");
        public static readonly MetricDefinition<IList<string>> SampleValuesString = new MetricDefinition<IList<string>>("sample_values");
        public static readonly MetricDefinition<IList<DateTime>> SampleValuesDateTime = new MetricDefinition<IList<DateTime>>("sample_values");
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
        public static readonly MetricDefinition<TextData> TextData = new MetricDefinition<TextData>("text_data");

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
