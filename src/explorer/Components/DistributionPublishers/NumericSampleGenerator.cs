namespace Explorer.Components
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Explorer.Metrics;

    public class NumericSampleGenerator : ExplorerComponentBase, PublisherComponent
    {
        private readonly ResultProvider<DistinctValuesComponent.Result> distinctValuesProvider;
        private readonly ResultProvider<SampleValuesGeneratorConfig.Result> sampleValuesGeneratorConfigProvider;
        private readonly ResultProvider<NumericDistribution> distributionProvider;

        public NumericSampleGenerator(
            ResultProvider<DistinctValuesComponent.Result> distinctValuesProvider,
            ResultProvider<SampleValuesGeneratorConfig.Result> sampleValuesGeneratorConfigProvider,
            ResultProvider<NumericDistribution> distributionProvider)
        {
            this.distinctValuesProvider = distinctValuesProvider;
            this.sampleValuesGeneratorConfigProvider = sampleValuesGeneratorConfigProvider;
            this.distributionProvider = distributionProvider;
        }

        public async IAsyncEnumerable<ExploreMetric> YieldMetrics()
        {
            var distinctValues = await distinctValuesProvider.ResultAsync;
            if (distinctValues == null)
            {
                yield break;
            }

            var config = await sampleValuesGeneratorConfigProvider.ResultAsync;
            if (config == null)
            {
                yield break;
            }

            if (config.CategoricalSampling)
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
                        .Generate(config.NumValuesToPublish)
                        .Select(s => Context.ColumnInfo.Type == Diffix.DValueType.Real ? s : Convert.ToInt64(s))
                        .OrderBy(_ => _)
                        .ToList());
        }
    }
}
