namespace Explorer.Explorers
{
    using Diffix;
    using Explorer.Common;
    using Explorer.Explorers.Components;

    internal class NumericColumnExplorer : ExplorerBase
    {
        public NumericColumnExplorer(DConnection conn, ExplorerContext ctx)
        : base(conn, ctx)
        {
        }

        protected override void InitializeComponents()
        {
            Initialize<SimpleStats<double>, SimpleStats<double>.Result>()
                .LinkToDependentComponent((NumericHistogramComponent)
                    Initialize<NumericHistogramComponent, NumericHistogramComponent.Result>()
                    .LinkToDependentComponent(
                        Initialize<QuartileEstimator, QuartileEstimator.Result>())
                    .LinkToDependentComponent(
                        Initialize<AverageEstimator, AverageEstimator.Result>()));

            // Initialize<DistinctValuesComponent, DistinctValuesComponent.Result>();
            Initialize<MinMaxRefiner, MinMaxRefiner.Result>();
        }
    }
}