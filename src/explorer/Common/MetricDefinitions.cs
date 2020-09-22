namespace Explorer.Common
{
    using System.Collections.Generic;

    public static class MetricDefinitions
    {
        public static readonly MetricDefinition<decimal> AverageEstimate = new MetricDefinition<decimal>("average_estimate");
        public static readonly MetricDefinition<IEnumerable<object>> SampleValues = new MetricDefinition<IEnumerable<object>>("sample_values");
    }
}
