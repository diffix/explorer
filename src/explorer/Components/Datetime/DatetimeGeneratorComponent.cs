namespace Explorer.Components
{
    using System.Collections.Generic;
    using System.Linq;

    using Explorer.Metrics;
    using Microsoft.Extensions.Options;

    public class DatetimeGeneratorComponent : PublisherComponent
    {
        private readonly ResultProvider<DatetimeDistribution> distributionProvider;
        private readonly ExplorerContext context;

        public DatetimeGeneratorComponent(
            ResultProvider<DatetimeDistribution> distributionProvider,
            ExplorerContext context)
        {
            this.distributionProvider = distributionProvider;
            this.context = context;
        }

        private int SamplesToPublish => context.SamplesToPublish;

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