namespace Explorer.Components
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Accord.Statistics.Distributions.Univariate;
    using Explorer.Common;
    using Explorer.Metrics;

    public class NumericSampleGenerator : EmpiricalDistributionPublisher
    {
        public const int DefaultSamplesToPublish = 20;
        private readonly ExplorerContext ctx;

        public NumericSampleGenerator(
            ExplorerContext ctx,
            ResultProvider<EmpiricalDistribution> distributionProvider)
        : base(distributionProvider)
        {
            this.ctx = ctx;
        }

        public int SamplesToPublish { get; set; } = DefaultSamplesToPublish;

        protected override IEnumerable<ExploreMetric> EnumerateMetrics(EmpiricalDistribution distribution)
        {
            yield return new UntypedMetric(
                name: "sample_values",
                metric: new
                {
                    Count = SamplesToPublish,
                    Samples = distribution
                        .Generate(SamplesToPublish)
                        .Select(s => ctx.ColumnType == Diffix.DValueType.Real ? s : Convert.ToInt64(s))
                        .OrderBy(_ => _),
                });
        }
    }
}