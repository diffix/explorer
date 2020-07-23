namespace Explorer.Components
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Explorer.Common;
    using Explorer.Metrics;

    public class NumericSampleGenerator : PublisherComponent
    {
        public const int DefaultSamplesToPublish = 20;
        private readonly ExplorerContext ctx;
        private readonly ResultProvider<DistinctValuesComponent.Result> distinctValuesProvider;
        private readonly ResultProvider<NumericDistribution> distributionProvider;

        public NumericSampleGenerator(
            ExplorerContext ctx,
            ResultProvider<DistinctValuesComponent.Result> distinctValuesProvider,
            ResultProvider<NumericDistribution> distributionProvider)
        {
            this.distinctValuesProvider = distinctValuesProvider;
            this.distributionProvider = distributionProvider;
            this.ctx = ctx;
        }

        public int SamplesToPublish { get; set; } = DefaultSamplesToPublish;

        public async IAsyncEnumerable<ExploreMetric> YieldMetrics()
        {
            var distinctValues = await distinctValuesProvider.ResultAsync;
            if (!distinctValues.IsCategorical)
            {
                var distribution = await distributionProvider.ResultAsync;

                yield return new UntypedMetric(
                    name: "sample_values",
                    metric: distribution
                            .Generate(SamplesToPublish)
                            .Select(s => ctx.ColumnInfo.Type == Diffix.DValueType.Real ? s : Convert.ToInt64(s))
                            .OrderBy(_ => _)
                            .ToArray());
            }
        }
    }
}
