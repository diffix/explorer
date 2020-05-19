namespace Explorer.Components
{
    using System.Collections.Generic;

    using Explorer.Metrics;

    public class DefaultBlobPublisher<T> : PublisherComponent<T>
    {
        public DefaultBlobPublisher(T toPublish)
        : base(new RawResultProvider<T>(toPublish))
        {
        }

        public override IEnumerable<ExploreMetric> YieldMetrics(T result)
        {
            yield return (result is null)
                ? new UntypedMetric($"metric.raw.{nameof(T)}", "null")
                : new UntypedMetric($"metric.raw.{nameof(T)}", result);
        }
    }
}