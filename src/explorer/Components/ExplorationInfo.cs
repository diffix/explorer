namespace Explorer.Components
{
    using System.Collections.Generic;

    using Explorer.Common;
    using Explorer.Metrics;

    public class ExplorationInfo : PublisherComponent
    {
        private readonly ExplorerContext ctx;

        public ExplorationInfo(ExplorerContext ctx)
        {
            this.ctx = ctx;
        }

        public async IAsyncEnumerable<ExploreMetric> YieldMetrics()
        {
            yield return new UntypedMetric("exploration_info", new
            {
                ctx.DataSource,
                ctx.Table,
                ctx.Column,
            });
        }
    }
}