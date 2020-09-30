namespace Explorer.Components
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.Json;
    using System.Threading.Tasks;

    using Explorer.Common;
    using Explorer.Common.Utils;

    public class CategoricalSampleGenerator : PublisherComponent
    {
        public const int DefaultSamplesToPublish = 20;

        private static readonly JsonElement JsonNull = JsonDocument.Parse("null").RootElement;

        private readonly ResultProvider<DistinctValuesComponent.Result> distinctValuesProvider;

        public CategoricalSampleGenerator(ResultProvider<DistinctValuesComponent.Result> distinctValuesProvider)
        {
            this.distinctValuesProvider = distinctValuesProvider;
        }

        public int NumValuesToPublish { get; set; } = DefaultSamplesToPublish;

        public static IList<JsonElement> GenerateSampleData(DistinctValuesComponent.Result distinctValuesResult, int count)
        {
            var allValues = ValueWithCountList<JsonElement>.FromValueWithCountEnum(
                distinctValuesResult
                    .DistinctRows
                    .Where(r => !r.IsSuppressed)
                    .Select(r => r.IsNull
                        ? ValueWithCountRow<JsonElement>.ValueCount(JsonNull, r.Count, r.CountNoise)
                        : r));

            var rand = new Random(Environment.TickCount);
            return Enumerable
                .Range(0, count)
                .Select(_ => allValues.GetRandomValue(rand))
                .ToList();
        }

        public async IAsyncEnumerable<ExploreMetric> YieldMetrics()
        {
            var distinctValuesResult = await distinctValuesProvider.ResultAsync;
            if (distinctValuesResult == null)
            {
                yield break;
            }
            if (!distinctValuesResult.IsCategorical)
            {
                yield break;
            }

            var sampleValues = GenerateSampleData(distinctValuesResult, NumValuesToPublish);
            yield return ExploreMetric.Create(MetricDefinitions.SampleValues, sampleValues);
        }
    }
}