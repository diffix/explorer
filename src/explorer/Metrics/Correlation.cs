namespace Explorer.Metrics
{
    using System.Collections.Generic;

    public class Correlation
    {
        public static readonly string MetricName = "correlation";

        public Correlation(string[] columns, double correlationFactor)
        {
            Columns = new List<string>(columns);
            CorrelationFactor = correlationFactor;
        }

        public List<string> Columns { get; }

        public double CorrelationFactor { get; }

        public ExploreMetric AsMetric() => new UntypedMetric(MetricName, this);
    }
}