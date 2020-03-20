namespace Explorer
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    internal class ExploreResult
    {
        public ExploreResult(Guid explorationId, ExploreStatus status)
        {
            Id = explorationId;
            Status = status;
            Metrics = Array.Empty<Metric>();
        }

        public ExploreResult(Guid explorationId, ExploreStatus status, IEnumerable<Metric> metrics)
        {
            Id = explorationId;
            Status = status;
            Metrics = metrics;
        }

#pragma warning disable SA1602 // Enumeration items should be documented
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public enum ExploreStatus
        {
            New,
            Processing,
            Complete,
            Canceled,
            Error,
        }
#pragma warning restore

        public ExploreStatus Status { get; }

        public IEnumerable<Metric> Metrics { get; }

        public Guid Id { get; }

        public class Metric
        {
            public Metric(string name, object value)
            {
                MetricName = name;
                MetricValue = value;
            }

            [JsonPropertyName("name")]
            public string MetricName { get; set; }

            [JsonPropertyName("value")]
            public object MetricValue { get; set; }
        }
    }
}
