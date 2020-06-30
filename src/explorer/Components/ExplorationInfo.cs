namespace Explorer.Components
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using Explorer.Common;
    using Explorer.Metrics;

    public class ExplorationInfo
        : ExplorerComponent<bool>, PublisherComponent
    {
        private readonly ExplorerContext ctx;

        public ExplorationInfo(ExplorerContext ctx)
        {
            this.ctx = ctx;
        }

        public async IAsyncEnumerable<ExploreMetric> YieldMetrics()
        {
            await ResultAsync;
            yield return new UntypedMetric("exploration_info", new
            {
                ctx.DataSource,
                ctx.Table,
                ctx.Column,
            });
        }

        protected override Task<bool> Explore()
        {
            return Task.FromResult(true);
        }
    }
}