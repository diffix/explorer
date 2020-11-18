namespace Explorer.Components
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    using Accord.Statistics.Distributions.Univariate;
    using Explorer.Common.JsonConversion;

    public class NumericDistribution : SampleDistribution<double>
    {
        private readonly EmpiricalDistribution distribution;

        public NumericDistribution(EmpiricalDistribution distribution)
        {
            this.distribution = distribution;
        }

        public double? Entropy
        {
            get
            {
                try
                {
                    return distribution.Entropy;
                }
                catch (InvalidOperationException)
                {
                    return null;
                }
            }
        }

        public double Mean => distribution.Mean;

        public double Mode => distribution.Mode;

        [JsonConverter(typeof(ThreeDoublesTupleConverter))]
        public (double, double, double) Quartiles => (
            distribution.Quartiles.Min,
            distribution.Median,
            distribution.Quartiles.Max);

        public double StandardDeviation => distribution.StandardDeviation;

        public double Variance => distribution.Variance;

        public IEnumerable<double> Generate(int numSamples) => distribution.Generate(numSamples);

        public double Generate(Random source) => distribution.Generate(source);

        public int? GenerateInt(Random source)
        {
            var d = distribution.Generate(source);
            if (double.IsFinite(d))
            {
                return Convert.ToInt32(d);
            }

            return null;
        }
    }
}