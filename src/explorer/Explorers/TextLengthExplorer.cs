namespace Explorer.Explorers
{
    using System.Threading.Tasks;

    using Diffix;
    using Explorer.Common;

    internal class TextLengthExplorer : IntegerColumnExplorer
    {
        private readonly string metricNamePrefix = "text.length";

        public TextLengthExplorer(string metricNamePrefix = "")
        {
            this.metricNamePrefix = metricNamePrefix;
        }

        public new async Task Explore(DConnection conn, ExplorerContext ctx)
        {
            var lengthContext = new ExplorerContext(ctx.Table, $"length({ctx.Column})", ctx.ColumnType);

            await base.Explore(conn, lengthContext);
        }

        public override void PublishMetric(ExploreMetric metric)
        {
            if (!string.IsNullOrEmpty(metricNamePrefix))
            {
                metric = new UntypedMetric(metricNamePrefix + "." + metric.Name, metric.Metric);
            }
            base.PublishMetric(metric);
        }
    }
}
