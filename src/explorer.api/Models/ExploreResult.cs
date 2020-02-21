namespace Explorer
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    internal class ExploreResult
    {
        public ExploreResult(Guid explorationId, string status)
        {
            Id = explorationId;
            Status = status;
            Metrics = Array.Empty<Metric>();
        }

        public ExploreResult(Guid explorationId, string status, IEnumerable<Metric> metrics)
        {
            Id = explorationId;
            Status = status;
            Metrics = metrics;
        }

        public string Status { get; }

        public IEnumerable<Metric> Metrics { get; }

        public Guid Id { get; }

        internal class Metric
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
