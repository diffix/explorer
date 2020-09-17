namespace Explorer.Components
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using Diffix;
    using Explorer.Metrics;

    public class ExplorationInfo : ExplorerComponentBase, PublisherComponent
    {
        public string DataSource => Context.DataSource;

        public string Table => Context.Table;

        public string Column => Context.Column;

        public DValueType ColumnType => Context.ColumnInfo.Type;

        public async IAsyncEnumerable<ExploreMetric> YieldMetrics()
        {
            await Task.CompletedTask;
            yield return new UntypedMetric("exploration_info", new
            {
                DataSource,
                Table,
                Column,
                ColumnType,
            });
        }
    }
}