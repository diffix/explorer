namespace Explorer.Api
{
    using System;

    using Diffix;
    using Explorer.Components;

    public static class ComponentComposition
    {
        public static Action<ExplorationConfig> ColumnConfiguration(DValueType columnType) =>
            columnType switch
            {
                DValueType.Integer => NumericExploration,
                DValueType.Real => NumericExploration,
                DValueType.Text => TextExploration,
                DValueType.Timestamp => DatetimeExploration,
                DValueType.Date => DatetimeExploration,
                DValueType.Datetime => DatetimeExploration,
                DValueType.Bool => _ => _.AddPublisher<DistinctValuesComponent>(),
                DValueType.Unknown => throw new ArgumentException(
                    $"Cannot explore column type {columnType}.", nameof(columnType)),
            };

        private static void NumericExploration(ExplorationConfig config)
        {
            config.AddPublisher<NumericHistogramComponent>();
            config.AddPublisher<QuartileEstimator>();
            config.AddPublisher<AverageEstimator>();
            config.AddPublisher<MinMaxRefiner>();
            config.AddPublisher<DistinctValuesComponent>();
            config.AddPublisher<NumericSampleGenerator>();
            config.AddPublisher<DistributionAnalysisComponent>();
        }

        private static void TextExploration(ExplorationConfig config)
        {
            config.AddPublisher<DistinctValuesComponent>();
            config.AddPublisher<EmailCheckComponent>();
            config.AddPublisher<TextGeneratorComponent>();
            config.AddPublisher<TextLengthComponent>();
        }

        private static void DatetimeExploration(ExplorationConfig config)
        {
            config.AddPublisher<DistinctValuesComponent>();
            config.AddPublisher<LinearTimeBuckets>();
            config.AddPublisher<CyclicalTimeBuckets>();
        }
    }
}