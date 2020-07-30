namespace Explorer.Components
{
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    using Diffix;
    using Diffix.JsonConversion;
    using Explorer.Common;
    using Explorer.Metrics;

    public class ExplorationInfo : PublisherComponent
    {
        private readonly ExplorerContext ctx;

        public ExplorationInfo(ExplorerContext ctx)
        {
            this.ctx = ctx;
        }

        public string DataSource => ctx.DataSource;

        public string Table => ctx.Table;

        public string Column => ctx.Column;

        [JsonConverter(typeof(DValueTypeEnumConverter))]
        public DValueType ColumnType { get => ctx.ColumnInfo.Type; }

        public async IAsyncEnumerable<ExploreMetric> YieldMetrics()
        {
            yield return new UntypedMetric("exploration_info", this);
        }
    }
}