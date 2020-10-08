namespace Explorer.Metrics
{
    using System.Collections.Generic;

    public class Correlation : ExploreMetric
    {
        public Correlation(string[] columns, double correlationFactor)
        {
            Columns = new List<string>(columns);
            CorrelationFactor = correlationFactor;
        }

        public string Name => "correlation";

        public object Metric => this;

        public int Priority => default;

        public List<string> Columns { get; }

        public double CorrelationFactor { get; }
    }
}