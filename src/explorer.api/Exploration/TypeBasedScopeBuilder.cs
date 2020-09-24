namespace Explorer.Api
{
    using System;
    using Diffix;
    using Explorer.Components;

    public sealed class TypeBasedScopeBuilder : ExplorationScopeBuilder
    {
        protected override void Configure(ExplorationScope scope, ExplorerContext context)
        {
            if (context.Columns.Length != 1)
            {
                throw new InvalidOperationException(
                    $"{nameof(TypeBasedScopeBuilder)} expects a single-column context, got {context.Columns.Length} columns.");
            }
            CommonConfiguration(scope);
            ColumnConfiguration(scope, context);
        }

        private static void CommonConfiguration(ExplorationScope scope)
        {
            scope.AddPublisher<ExplorationInfoComponent>();
            scope.AddPublisher<DistinctValuesComponent>();
            scope.AddPublisher<CategoricalSampleGenerator>();
        }

        // Disabling this because the compiler can't infer Action<ExplorationScope>.
#pragma warning disable IDE0007 // Use var instead of explicit type
        private static void ColumnConfiguration(ExplorationScope scope, ExplorerContext context)
        {
            Action<ExplorationScope> configure = context.ColumnInfo.Type switch
            {
                DValueType.Integer => NumericExploration,
                DValueType.Real => NumericExploration,
                DValueType.Text => TextExploration,
                DValueType.Timestamp => DatetimeExploration,
                DValueType.Date => DatetimeExploration,
                DValueType.Datetime => DatetimeExploration,
                DValueType.Bool => BoolExploration,
                _ => throw new InvalidOperationException($"Cannot explore column type {context.ColumnInfo.Type}."),
            };

            configure(scope);
        }
#pragma warning restore IDE0007 // Use var instead of explicit type

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
            scope.AddPublisher<TextLengthDistributionComponent>();
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