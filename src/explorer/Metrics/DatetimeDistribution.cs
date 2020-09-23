namespace Explorer.Metrics
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.Json.Serialization;

    using Accord.Statistics.Distributions.Univariate;
    using Explorer.Common.JsonConversion;

    public class DatetimeDistribution : SampleDistribution<DateTime>
    {
        private readonly string timeUnit;
        private readonly EmpiricalDistribution distribution;

        public DatetimeDistribution(string timeUnit, EmpiricalDistribution distribution)
        {
            this.timeUnit = timeUnit;
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

        public DateTime Mean { get => ConvertTime(distribution.Mean); }

        public DateTime Mode { get => ConvertTime(distribution.Mode); }

        [JsonConverter(typeof(ThreeDateTimesTupleConverter))]
        public (DateTime, DateTime, DateTime) Quartiles
        {
            get => (
                ConvertTime(distribution.Quartiles.Min),
                ConvertTime(distribution.Median),
                ConvertTime(distribution.Quartiles.Max));
        }

        public double StandardDeviation { get => distribution.StandardDeviation; }

        public double Variance { get => distribution.Variance; }

        public IEnumerable<DateTime> Generate(int numSamples) =>
            distribution.Generate(numSamples).Select(ConvertTime);

        private DateTime ConvertTime(double time) =>
            DateTime.UnixEpoch + timeUnit switch
            {
                "hour" => TimeSpan.FromHours(Math.Round(time)),
                "minute" => TimeSpan.FromMinutes(Math.Round(time)),
                "second" => TimeSpan.FromSeconds(Math.Round(time)),
                _ => TimeSpan.FromDays(Math.Round(time)),
            };
    }
}