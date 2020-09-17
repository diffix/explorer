namespace Explorer.Components
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Explorer.Common;

    public class NumericSampleGenerator : ExplorerComponentBase, PublisherComponent
    {
        public const int DefaultSamplesToPublish = 20;
        private readonly ResultProvider<DistinctValuesComponent.Result> distinctValuesProvider;
        private readonly ResultProvider<NumericDistribution> distributionProvider;

        public NumericSampleGenerator(
            ResultProvider<DistinctValuesComponent.Result> distinctValuesProvider,
            ResultProvider<NumericDistribution> distributionProvider)
        {
            this.distinctValuesProvider = distinctValuesProvider;
            this.distributionProvider = distributionProvider;
        }

        public int SamplesToPublish { get; set; } = DefaultSamplesToPublish;

        public async IAsyncEnumerable<ExploreMetric> YieldMetrics()
        {
            var distinctValues = await distinctValuesProvider.ResultAsync;
            if (distinctValues == null)
            {
                yield break;
            }
            if (distinctValues.IsCategorical)
            {
                yield break;
            }

            var distribution = await distributionProvider.ResultAsync;
            if (distribution == null)
            {
                yield break;
            }

            yield return new UntypedMetric(
                name: "sample_values",
                metric: distribution
                        .Generate(SamplesToPublish)
                        .Select(s => Context.ColumnInfo.Type == Diffix.DValueType.Real ? s : Convert.ToInt64(s))
                        .OrderBy(_ => _)
                        .ToList());
        }
    }
}
