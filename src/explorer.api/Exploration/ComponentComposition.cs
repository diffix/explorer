namespace Explorer.Api
{
    using System;

    using Diffix;
    using Explorer.Components;

    public class ComponentComposition : ExplorationConfigurator
    {
        private readonly ExplorerContext ctx;

        public ComponentComposition(ExplorerContext ctx)
        {
            this.ctx = ctx;
        }

        public void Configure(ExplorationScope scope)
        {
            scope.UseContext(ctx);
            CommonConfiguration(scope);
            ColumnConfiguration(scope);
        }

        public void ColumnConfiguration(ExplorationScope scope)
        {
            Action<ExplorationScope> configure = ctx.ColumnInfo.Type switch
            {
                DValueType.Integer => NumericExploration,
                DValueType.Real => NumericExploration,
                DValueType.Text => TextExploration,
                DValueType.Timestamp => DatetimeExploration,
                DValueType.Date => DatetimeExploration,
                DValueType.Datetime => DatetimeExploration,
                DValueType.Bool => BoolExploration,
                _ => throw new InvalidOperationException($"Cannot explore column type {ctx.ColumnInfo.Type}."),
            };

            configure(scope);
        }

        private static void CommonConfiguration(ExplorationScope scope)
        {
            scope.AddPublisher<ExplorationInfo>();
            scope.AddPublisher<DistinctValuesComponent>();
            scope.AddPublisher<CategoricalSampleGenerator>();
        }

        private static void BoolExploration(ExplorationScope scope)
        {
        }

        private static void NumericExploration(ExplorationScope scope)
        {
            scope.AddPublisher<HistogramSelectorComponent>();
            scope.AddPublisher<MinMaxFromHistogramComponent>();
            scope.AddPublisher<QuartileEstimator>();
            scope.AddPublisher<AverageEstimator>();
            scope.AddPublisher<MinMaxRefiner>();
            scope.AddPublisher<NumericSampleGenerator>();
            scope.AddPublisher<DistributionAnalysisComponent>();
            scope.AddPublisher<DescriptiveStatsPublisher>();
        }

        private static void TextExploration(ExplorationScope scope)
        {
            scope.AddPublisher<EmailCheckComponent>();
            scope.AddPublisher<TextGeneratorComponent>();
            scope.AddPublisher<TextLengthComponent>();
        }

        private static void DatetimeExploration(ExplorationScope scope)
        {
            scope.AddPublisher<LinearTimeBuckets>();
            scope.AddPublisher<CyclicalTimeBuckets>();
            scope.AddPublisher<DatetimeDistributionComponent>();
            scope.AddPublisher<DatetimeGeneratorComponent>();
        }
    }
}