namespace Explorer.Components
{
    using System.Collections.Generic;
    using Accord.Statistics.Distributions.Univariate;

    public class NumericDistribution : SampleDistribution<double>
    {
        private readonly EmpiricalDistribution distribution;

        public NumericDistribution(EmpiricalDistribution distribution)
        {
            this.distribution = distribution;
        }

        public double Entropy => distribution.Entropy;

        public double Mean => distribution.Mean;

        public double Mode => distribution.Mode;

        public (double, double, double) Quartiles => (
            distribution.Quartiles.Min,
            distribution.Median,
            distribution.Quartiles.Max);

        public double StandardDeviation => distribution.StandardDeviation;

        public double Variance => distribution.Variance;

        public IEnumerable<double> Generate(int numSamples) => distribution.Generate(numSamples);
    }
}