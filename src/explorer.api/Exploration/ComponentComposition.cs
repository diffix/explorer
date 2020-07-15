namespace Explorer.Api
{
    using System;

    using Diffix;
    using Explorer.Components;

    public static class ComponentComposition
    {
        public static Action<ExplorationConfig> ColumnConfiguration(DValueType columnType)
        {
            Action<ExplorationConfig> typeBasedConfiguration = columnType switch
            {
                DValueType.Integer => NumericExploration,
                DValueType.Real => NumericExploration,
                DValueType.Text => TextExploration,
                DValueType.Timestamp => DatetimeExploration,
                DValueType.Date => DatetimeExploration,
                DValueType.Datetime => DatetimeExploration,
                DValueType.Bool => BoolExploration,
                _ => throw new ArgumentException(
                    $"Cannot explore column type {columnType}.", nameof(columnType)),
            };

            return config =>
            {
                CommonConfiguration(config);
                typeBasedConfiguration(config);
            };
        }

        private static void CommonConfiguration(ExplorationConfig config)
        {
            config.AddPublisher<ExplorationInfo>();
            config.AddPublisher<DistinctValuesComponent>();
            config.AddPublisher<CategoricalSampleGenerator>();
        }

        private static void BoolExploration(ExplorationConfig config)
        {
        }

        private static void NumericExploration(ExplorationConfig config)
        {
            config.AddPublisher<HistogramSelectorComponent>();
            config.AddPublisher<MinMaxFromHistogramComponent>();
            config.AddPublisher<QuartileEstimator>();
            config.AddPublisher<AverageEstimator>();
            config.AddPublisher<MinMaxRefiner>();
            config.AddPublisher<NumericSampleGenerator>();
            config.AddPublisher<DistributionAnalysisComponent>();
            config.AddPublisher<DescriptiveStatsPublisher>();
        }

        private static void TextExploration(ExplorationConfig config)
        {
            config.AddPublisher<EmailCheckComponent>();
            config.AddPublisher<TextGeneratorComponent>();
            config.AddPublisher<TextLengthComponent>();
        }

        private static void DatetimeExploration(ExplorationConfig config)
        {
            config.AddPublisher<LinearTimeBuckets>();
            config.AddPublisher<CyclicalTimeBuckets>();
            config.AddPublisher<DatetimeDistributionComponent>();
            config.AddPublisher<DatetimeGeneratorComponent>();
        }
    }
}