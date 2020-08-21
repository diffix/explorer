namespace Explorer.Components
{
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;
    using System.Threading.Tasks;

    using Diffix;
    using Explorer.Metrics;

    public class ExplorationInfo : ExplorerComponentBase, PublisherComponent
    {
        public string DataSource => Context.DataSource;

        public string Table => Context.Table;

        public ImmutableArray<string> Columns => Context.Columns;

        public ImmutableArray<DValueType> ColumnTypes => Context.ColumnInfos.Select(ci => ci.Type).ToImmutableArray();

        public async IAsyncEnumerable<ExploreMetric> YieldMetrics()
        {
            await Task.CompletedTask;
            yield return new UntypedMetric("exploration_info", new
            {
                DataSource,
                Table,
                Columns,
                ColumnTypes,
            });
        }
    }
}