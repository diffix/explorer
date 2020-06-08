namespace Explorer.Components
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using Accord.Statistics.Analysis;
    using Accord.Statistics.Distributions.Univariate;
    using Explorer.Metrics;

    public class DistributionAnalysisComponent : ExplorerComponent<GoodnessOfFitCollection>, PublisherComponent
    {
        private readonly ResultProvider<EmpiricalDistribution> distributionProvider;

        public DistributionAnalysisComponent(ResultProvider<EmpiricalDistribution> distributionProvider)
        {
            this.distributionProvider = distributionProvider;
        }

        public async IAsyncEnumerable<ExploreMetric> YieldMetrics()
        {
            var fits = await ResultAsync;

            var goodFits = fits.Values
                .Where(fit =>
                    fit.AndersonDarlingRank == 0 ||
                    fit.ChiSquareRank == 0 ||
                    fit.KolmogorovSmirnovRank == 0);

            yield return new UntypedMetric(
                name: "distribution_estimates",
                metric: goodFits.Select(fit =>
                {
                    var ad = fit.Analysis.AndersonDarling[fit.Index];
                    var cs = fit.Analysis.ChiSquare[fit.Index];
                    var ks = fit.Analysis.KolmogorovSmirnov[fit.Index];

                    return new
                    {
                        fit.Name,
                        Distribution = fit.Distribution.ToString(),
                        Goodness = new object?[]
                        {
                            ad is null ? null : new
                            {
                                Method = "AndersonDarling",
                                ad.PValue,
                                ad.Significant,
                                Rank = fit.AndersonDarlingRank,
                            },
                            cs is null ? null : new
                            {
                                Method = "ChiSquare",
                                cs.PValue,
                                cs.Significant,
                                Rank = fit.ChiSquareRank,
                            },
                            ks is null ? null : new
                            {
                                Method = "KolmogorovSmirnov",
                                ks.PValue,
                                ks.Significant,
                                Rank = fit.KolmogorovSmirnovRank,
                            },
                        }
                        .Where(o => !(o is null)),
                    };
                }));
        }

        protected override async Task<GoodnessOfFitCollection> Explore()
        {
            var distribution = await distributionProvider.ResultAsync;

            return await Task.Run(() =>
            {
                var analysis = new DistributionAnalysis();
                return analysis.Learn(distribution.Generate(10_000));
            });
        }
    }
}