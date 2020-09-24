namespace Explorer.Components
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using Explorer.Common;
    using Explorer.Metrics;

    public class ExplorationInfoComponent : ExplorerComponentBase, PublisherComponent
    {
        public async IAsyncEnumerable<ExploreMetric> YieldMetrics()
        {
            await Task.CompletedTask;
            var info = new ExplorationInfo(Context.DataSource, Context.Table, Context.Column, Context.ColumnInfo.Type);
            yield return ExploreMetric.Create(MetricDefinitions.ExplorationInfo, info);
        }
    }
}