namespace Explorer.Common
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using Explorer.Explorers.Components;
    using Explorer.Explorers.Metrics;

    internal class ExplorerBase
    {
        private readonly IEnumerable<ResultProvider<MetricsProvider>> resultMetricsProviders;
        private readonly MetricsPublisher metricsPublisher;

        public ExplorerBase(
            MetricsPublisher metricsPublisher,
            IEnumerable<ResultProvider<MetricsProvider>> resultMetricsProviders)
        {
            this.metricsPublisher = metricsPublisher;
            this.resultMetricsProviders = resultMetricsProviders;
        }

        public async Task Explore()
        {
            var publishTasks = resultMetricsProviders.Select(rp =>
                Task.Run(async () =>
                {
                    var result = await rp.ResultAsync;
                    if (result is MetricsProvider provider)
                    {
                        await metricsPublisher.PublishMetricsAsync(provider);
                    }
                }));

            await Task.WhenAll(publishTasks);
        }
    }
}