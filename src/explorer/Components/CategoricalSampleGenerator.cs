namespace Explorer.Components
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.Json;
    using System.Threading.Tasks;

    using Explorer.Common;
    using Explorer.Metrics;

    public class CategoricalSampleGenerator
        : ExplorerComponent<CategoricalSampleGenerator.Result>, PublisherComponent
    {
        private static readonly JsonElement JsonNull = Utilities.MakeJsonNull();

        private readonly ResultProvider<DistinctValuesComponent.Result> distinctValuesProvider;
        private readonly ResultProvider<SampleValuesGeneratorConfig.Result> sampleValuesGeneratorConfigProvider;

        public CategoricalSampleGenerator(
            ResultProvider<DistinctValuesComponent.Result> distinctValuesProvider,
            ResultProvider<SampleValuesGeneratorConfig.Result> sampleValuesGeneratorConfigProvider)
        {
            this.distinctValuesProvider = distinctValuesProvider;
            this.sampleValuesGeneratorConfigProvider = sampleValuesGeneratorConfigProvider;
        }

        public async IAsyncEnumerable<ExploreMetric> YieldMetrics()
        {
            var result = await ResultAsync;
            if (result?.SampleValues.Count > 0)
            {
                yield return new UntypedMetric(name: "sample_values", metric: result.SampleValues);
            }
        }

        protected override async Task<Result?> Explore()
        {
            var distinctValuesResult = await distinctValuesProvider.ResultAsync;
            if (distinctValuesResult == null)
            {
                return null;
            }

            var config = await sampleValuesGeneratorConfigProvider.ResultAsync;
            if (config == null)
            {
                return null;
            }

            var sampleValues = Enumerable.Empty<JsonElement>();
            if (config.CategoricalSampling)
            {
                var rand = new Random(Environment.TickCount);
                var allValues = ValueWithCountList<JsonElement>.FromValueWithCountEnum(
                    distinctValuesResult
                        .DistinctRows
                        .Where(r => !r.IsSuppressed)
                        .Select(r => r.IsNull
                            ? ValueWithCount<JsonElement>.ValueCount(JsonNull, r.Count, r.CountNoise)
                            : r));

                sampleValues = Enumerable
                    .Range(0, config.SamplesToPublish)
                    .Select(_ => allValues.GetRandomValue(rand));
            }
            return new Result(sampleValues.ToList());
        }

        public class Result
        {
            public Result(IList<JsonElement> sampleValues)
            {
                SampleValues = sampleValues;
            }

            public IList<JsonElement> SampleValues { get; }
        }
    }
}