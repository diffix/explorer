namespace Explorer.Api
{
    using System;

    using Diffix;
    using Explorer.Components;

    public static class ComponentComposition
    {
        public static Action<ExplorationConfig> ColumnConfiguration(DValueType columnType)
        {
            return config =>
            {
                CommonConfiguration(config);
                switch (columnType)
                {
                    case DValueType.Integer:
                    case DValueType.Real: NumericExploration(config); break;
                    case DValueType.Timestamp:
                    case DValueType.Date:
                    case DValueType.Datetime: DatetimeExploration(config); break;
                    case DValueType.Bool: BoolExploration(config); break;
                    case DValueType.Text: TextExploration(config); break;
                    default:
                        throw new ArgumentException(
                            $"Cannot explore column type {columnType}.", nameof(columnType));
                }
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
            _ = config;
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