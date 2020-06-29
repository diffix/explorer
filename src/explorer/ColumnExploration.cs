namespace Explorer
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using Explorer.Metrics;

    public sealed class ColumnExploration
    {
        private readonly MetricsPublisher publisher;

        public ColumnExploration(ExplorationConfig config, MetricsPublisher publisher, string column)
        {
            this.publisher = publisher;
            Column = column;
            Completion = Task.WhenAll(config.Tasks);
        }

        public string Column { get; }

        public Task Completion { get; }

        public IEnumerable<ExploreMetric> PublishedMetrics => publisher.PublishedMetrics;
    }
}