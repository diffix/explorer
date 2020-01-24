namespace Explorer
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json.Serialization;
    using Aircloak.JsonApi.ResponseTypes;

    public class ExploreResult
    {
        public string Status { get; set; }

        public List<Metric> Metrics { get; set; }

        public Guid Id { get; set; }

        public class Metric
        {
            [JsonPropertyName("Name")]
            public string MetricName { get; set; }

            [JsonPropertyName("Type")]
            public AircloakType MetricType { get; set; }

            [JsonPropertyName("Value")]
            public object MetricValue { get; set; }
        }
    }

}
