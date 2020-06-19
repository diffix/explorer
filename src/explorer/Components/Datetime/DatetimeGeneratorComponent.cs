namespace Explorer.Components
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Explorer.Metrics;

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

            yield return new UntypedMetric(
                name: "sample_values",
                metric: new
                {
                    Count = SamplesToPublish,
                    Samples = distribution
                        .Generate(SamplesToPublish)
                        .OrderBy(_ => _),
                });
        }
    }
}