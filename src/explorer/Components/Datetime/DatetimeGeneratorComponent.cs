namespace Explorer.Components
{
    using System.Collections.Generic;
    using System.Linq;

    using Explorer.Common;

    public class DatetimeGeneratorComponent : PublisherComponent
    {
        public const int DefaultSamplesToPublish = 20;

        private readonly ResultProvider<DatetimeDistribution> distributionProvider;

        public DatetimeGeneratorComponent(ResultProvider<DatetimeDistribution> distributionProvider)
        {
            this.distributionProvider = distributionProvider;
        }

        public int SamplesToPublish { get; set; } = DefaultSamplesToPublish;

        public async IAsyncEnumerable<ExploreMetric> YieldMetrics()
        {
            var distribution = await distributionProvider.ResultAsync;
            if (distribution == null)
            {
                yield break;
            }

            yield return new UntypedMetric(
                name: "sample_values",
                metric: distribution
                        .Generate(SamplesToPublish)
                        .OrderBy(_ => _)
                        .ToList());
        }
    }
}