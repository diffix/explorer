namespace Explorer.Components
{
    using System.Collections.Generic;
    using System.Linq;

    using Accord.Statistics.Analysis;
    using Explorer.Common;
    using Explorer.Metrics;

    public class DistributionAnalysisComponent : PublisherComponent
    {
        private readonly ResultProvider<NumericDistribution> distributionProvider;

        public DistributionAnalysisComponent(ResultProvider<NumericDistribution> distributionProvider)
        {
            this.distributionProvider = distributionProvider;
        }

        public async IAsyncEnumerable<ExploreMetric> YieldMetrics()
        {
            var distribution = await distributionProvider.ResultAsync;
            if (distribution == null)
            {
                yield break;
            }

            var analysis = new DistributionAnalysis();
            var fits = analysis.Learn(distribution.Generate(10_000).ToArray());
            if (fits == null)
            {
                yield break;
            }

            var goodFits = fits.Values
                .Where(fit =>
                    fit.AndersonDarlingRank == 0 ||
                    fit.ChiSquareRank == 0 ||
                    fit.KolmogorovSmirnovRank == 0);

            var estimates = goodFits.Select(fit =>
            {
                var ad = fit.Analysis.AndersonDarling[fit.Index];
                var cs = fit.Analysis.ChiSquare[fit.Index];
                var ks = fit.Analysis.KolmogorovSmirnov[fit.Index];

                return new DistributionEstimate(
                    fit.Name,
                    fit.Distribution.ToString(),
                    new GoodnessMetric?[]
                    {
                        ad is null
                            ? null
                            : GoodnessMetric.AndersonDarling(ad, fit.AndersonDarlingRank),
                        cs is null
                            ? null
                            : GoodnessMetric.ChiSquare(cs, fit.ChiSquareRank),
                        ks is null
                            ? null
                            : GoodnessMetric.KolmogorovSmirnov(ks, fit.KolmogorovSmirnovRank),
                    }
                    .Where(gm => !(gm is null) && double.IsFinite(gm.PValue))
                    .Cast<GoodnessMetric>());
            });

            yield return ExploreMetric.Create(MetricDefinitions.DistributionEstimates, estimates.ToList());
        }
    }
}