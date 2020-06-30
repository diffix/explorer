namespace Explorer.Components
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using Explorer.Common;
    using Explorer.Metrics;

    public class ExplorationInfo
        : ExplorerComponent<ExplorationInfo.Result>, PublisherComponent
    {
        private readonly ExplorerContext ctx;

        public ExplorationInfo(ExplorerContext ctx)
        {
            this.ctx = ctx;
        }

        public async IAsyncEnumerable<ExploreMetric> YieldMetrics()
        {
            var r = await ResultAsync;
            yield return new UntypedMetric("exploration_info", new
            {
                r.DataSource,
                r.Table,
                r.Column,
            });
        }

        protected override Task<Result> Explore()
        {
            return Task.FromResult(new Result(ctx.DataSource, ctx.Table, ctx.Column));
        }

        public class Result
        {
            public Result(string dataSource, string table, string column)
            {
                DataSource = dataSource;
                Table = table;
                Column = column;
            }

            public string DataSource { get; }

            public string Table { get; }

            public string Column { get; }
        }
    }
}