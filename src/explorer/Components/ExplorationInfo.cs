namespace Explorer.Components
{
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    using Diffix;
    using Diffix.JsonConversion;
    using Explorer.Metrics;

    public class ExplorationInfo : ExplorerComponentBase, PublisherComponent
    {
        public string DataSource => Context.DataSource;

        public string Table => Context.Table;

        public string Column => Context.Column;

        public DValueType ColumnType => Context.ColumnInfo.Type;

#pragma warning disable CS1998 // This async method lacks 'await' operators and will run synchronously.
        public async IAsyncEnumerable<ExploreMetric> YieldMetrics()
        {
            yield return new UntypedMetric("exploration_info", new
            {
                DataSource,
                Table,
                Column,
                ColumnType,
            });
        }
#pragma warning restore CS1998 // This async method lacks 'await' operators and will run synchronously.
    }
}