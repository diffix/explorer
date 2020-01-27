namespace Explorer
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json.Serialization;
    using Aircloak.JsonApi.ResponseTypes;

    internal class ExploreResult
    {
        public ExploreResult(string status)
        {
            Status = status;
        }

        public string Status { get; set; }

        public List<Metric>? Metrics { get; }

        public Guid Id { get; set; }

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
