namespace Explorer
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json.Serialization;
    using Aircloak.JsonApi.ResponseTypes;

    internal class ExploreResult
    {
        public ExploreResult(Guid explorationId, string status)
        {
            Id = explorationId;
            Status = status;
            Metrics = new List<Metric>();
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
            public Metric(string name)
            {
                MetricName = name;
            }

            [JsonPropertyName("Name")]
            public string MetricName { get; set; }

            [JsonPropertyName("Type")]
            public AircloakType MetricType { get; set; }

            [JsonPropertyName("Value")]
            public object? MetricValue { get; set; }
        }
    }
}
