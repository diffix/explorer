namespace Explorer
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using Explorer.Metrics;
    using Lamar;

    public sealed class Exploration
    {
        private readonly MetricsPublisher publisher;

        public Exploration(ExplorationConfig config, MetricsPublisher publisher)
        {
            this.publisher = publisher;
            Completion = Task.WhenAll(config.Tasks);
        }

        public IEnumerable<ExploreMetric> PublishedMetrics => publisher.PublishedMetrics;

        public Task Completion { get; }

        public ExplorationStatus Status => ConvertToExplorationStatus(Completion.Status);

        public static Exploration Configure(INestedContainer scope, Action<ExplorationConfig> configure)
        {
            var config = new ExplorationConfig(scope);
            configure(config);
            return new Exploration(config, scope.GetInstance<MetricsPublisher>());
        }

        private static ExplorationStatus ConvertToExplorationStatus(TaskStatus status)
        {
            return status switch
            {
                TaskStatus.Canceled => ExplorationStatus.Canceled,
                TaskStatus.Created => ExplorationStatus.New,
                TaskStatus.Faulted => ExplorationStatus.Error,
                TaskStatus.RanToCompletion => ExplorationStatus.Complete,
                TaskStatus.Running => ExplorationStatus.Processing,
                TaskStatus.WaitingForActivation => ExplorationStatus.Processing,
                TaskStatus.WaitingToRun => ExplorationStatus.Processing,
                TaskStatus.WaitingForChildrenToComplete => ExplorationStatus.Processing,
                _ => throw new Exception("Unexpected TaskStatus: '{exploration.Status}'."),
            };
        }
    }
}