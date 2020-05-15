namespace Explorer.Components
{
    using System;
    using System.Collections.Generic;

    using Explorer.Metrics;

    public class InlinePublisher : PublisherComponent
    {
        private readonly Func<IAsyncEnumerable<ExploreMetric>> publisherAction;

        public InlinePublisher(Func<IAsyncEnumerable<ExploreMetric>> publisherAction)
        {
            this.publisherAction = publisherAction;
        }

        public override IAsyncEnumerable<ExploreMetric> YieldMetrics() => publisherAction();
    }
}