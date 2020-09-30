namespace Explorer.Common
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Text.Json;

    using Explorer.Metrics;

    public static class MetricDefinitions
    {
        public static readonly MetricDefinition<ExplorationInfo> ExplorationInfo = new MetricDefinition<ExplorationInfo>("explorationInfo");
        public static readonly MetricDefinition<IEnumerable> SampleValues = new MetricDefinition<IEnumerable>("sampleValues");
        public static readonly MetricDefinition<IList<JsonElement>> SampleValuesJsonElement = new MetricDefinition<IList<JsonElement>>("sampleValues");
        public static readonly MetricDefinition<IList<double>> SampleValuesDouble = new MetricDefinition<IList<double>>("sampleValues");
        public static readonly MetricDefinition<IList<string>> SampleValuesString = new MetricDefinition<IList<string>>("sampleValues");
        public static readonly MetricDefinition<IList<DateTime>> SampleValuesDateTime = new MetricDefinition<IList<DateTime>>("sampleValues");
        public static readonly MetricDefinition<bool> IsCategorical = new MetricDefinition<bool>("isCategorical");
        public static readonly MetricDefinition<decimal> Min = new MetricDefinition<decimal>("min");
        public static readonly MetricDefinition<decimal> Max = new MetricDefinition<decimal>("max");
        public static readonly MetricDefinition<long> SimpleStatsCount = new MetricDefinition<long>("count");
        public static readonly MetricDefinition<decimal> AverageEstimate = new MetricDefinition<decimal>("averageEstimate");
        public static readonly MetricDefinition<IList<double>> QuartileEstimates = new MetricDefinition<IList<double>>("quartileEstimates");
        public static readonly MetricDefinition<IList<DistributionEstimate>> DistributionEstimates = new MetricDefinition<IList<DistributionEstimate>>("distributionEstimates");
        public static readonly MetricDefinition<NumericDistribution> NumericDescriptiveStats = new MetricDefinition<NumericDistribution>("descriptiveStats");
        public static readonly MetricDefinition<DatetimeDistribution> DateTimeDescriptiveStats = new MetricDefinition<DatetimeDistribution>("descriptiveStats");
        public static readonly MetricDefinition<Histogram> Histogram = new MetricDefinition<Histogram>("histogram");
        public static readonly MetricDefinition<CategoricalData> CategoricalData = new MetricDefinition<CategoricalData>("categoricalData");
        public static readonly MetricDefinition<TextData> TextData = new MetricDefinition<TextData>("textData");

        public static readonly MetricDefinition<DateTimeMetric<int>> DatesCyclicalSecond = new MetricDefinition<DateTimeMetric<int>>("datesCyclicalSecond");
        public static readonly MetricDefinition<DateTimeMetric<int>> DatesCyclicalMinute = new MetricDefinition<DateTimeMetric<int>>("datesCyclicalMinute");
        public static readonly MetricDefinition<DateTimeMetric<int>> DatesCyclicalHour = new MetricDefinition<DateTimeMetric<int>>("datesCyclicalHour");
        public static readonly MetricDefinition<DateTimeMetric<int>> DatesCyclicalWeekday = new MetricDefinition<DateTimeMetric<int>>("datesCyclicalWeekday");
        public static readonly MetricDefinition<DateTimeMetric<int>> DatesCyclicalDay = new MetricDefinition<DateTimeMetric<int>>("datesCyclicalDay");
        public static readonly MetricDefinition<DateTimeMetric<int>> DatesCyclicalMonth = new MetricDefinition<DateTimeMetric<int>>("datesCyclicalMonth");
        public static readonly MetricDefinition<DateTimeMetric<int>> DatesCyclicalQuarter = new MetricDefinition<DateTimeMetric<int>>("datesCyclicalQuarter");
        public static readonly MetricDefinition<DateTimeMetric<int>> DatesCyclicalYear = new MetricDefinition<DateTimeMetric<int>>("datesCyclicalYear");

        public static readonly MetricDefinition<DateTimeMetric<DateTime>> DatesLinearSecond = new MetricDefinition<DateTimeMetric<DateTime>>("datesLinearSecond");
        public static readonly MetricDefinition<DateTimeMetric<DateTime>> DatesLinearMinute = new MetricDefinition<DateTimeMetric<DateTime>>("datesLinearMinute");
        public static readonly MetricDefinition<DateTimeMetric<DateTime>> DatesLinearHour = new MetricDefinition<DateTimeMetric<DateTime>>("datesLinearHour");
        public static readonly MetricDefinition<DateTimeMetric<DateTime>> DatesLinearDay = new MetricDefinition<DateTimeMetric<DateTime>>("datesLinearDay");
        public static readonly MetricDefinition<DateTimeMetric<DateTime>> DatesLinearMonth = new MetricDefinition<DateTimeMetric<DateTime>>("datesLinearMonth");
        public static readonly MetricDefinition<DateTimeMetric<DateTime>> DatesLinearQuarter = new MetricDefinition<DateTimeMetric<DateTime>>("datesLinearQuarter");
        public static readonly MetricDefinition<DateTimeMetric<DateTime>> DatesLinearYear = new MetricDefinition<DateTimeMetric<DateTime>>("datesLinearYear");

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
