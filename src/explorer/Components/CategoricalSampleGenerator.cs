namespace Explorer.Components
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.Json;
    using System.Threading.Tasks;

    using Diffix;
    using Explorer.Common;
    using Explorer.Metrics;

    public class CategoricalSampleGenerator
        : ExplorerComponent<CategoricalSampleGenerator.Result>, PublisherComponent
    {
        public const int DefaultSamplesToPublish = 20;

        private static readonly JsonElement JsonNullElement = JsonDocument.Parse("null").RootElement;
        private readonly DistinctValuesComponent distinctValues;

        public CategoricalSampleGenerator(DistinctValuesComponent distinctValues)
        {
            this.distinctValues = distinctValues;
        }

        public int NumValuesToPublish { get; set; } = DefaultSamplesToPublish;

        public async IAsyncEnumerable<ExploreMetric> YieldMetrics()
        {
            var result = await ResultAsync;
            if (result.SampleValues.Count > 0)
            {
                yield return new UntypedMetric(name: "sample_values", metric: result.SampleValues);
            }
        }

        protected override async Task<Result> Explore()
        {
            var distinctValuesResult = await distinctValues.ResultAsync;
            var sampleValues = Enumerable.Empty<JsonElement>();
            if (distinctValuesResult.IsCategorical)
            {
                var rand = new Random(Environment.TickCount);
                var allValues = ValueWithCountList<JsonElement>.FromValueWithCountEnum(distinctValuesResult.DistinctRows);
                sampleValues = Enumerable
                    .Range(0, NumValuesToPublish)
                    .Select(_ => allValues.GetRandomValue(rand, JsonNullElement));
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