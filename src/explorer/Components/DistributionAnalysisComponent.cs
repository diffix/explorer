namespace Explorer.Components
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using Accord.Statistics.Analysis;
    using Accord.Statistics.Testing;
    using Explorer.Common;

    public class DistributionAnalysisComponent : ExplorerComponent<GoodnessOfFitCollection>, PublisherComponent
    {
        private readonly ResultProvider<NumericDistribution> distributionProvider;

        public DistributionAnalysisComponent(ResultProvider<NumericDistribution> distributionProvider)
        {
            this.distributionProvider = distributionProvider;
        }

        public async IAsyncEnumerable<ExploreMetric> YieldMetrics()
        {
            var fits = await ResultAsync;
            if (fits == null)
            {
                yield break;
            }

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
                        Goodness = new GoodnessMetric?[]
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
                        .Where(gm => !(gm is null) && double.IsFinite(gm.PValue)),
                    };
                })
                .ToList());
        }

        protected override async Task<GoodnessOfFitCollection?> Explore()
        {
            var distribution = await distributionProvider.ResultAsync;
            if (distribution == null)
            {
                return null;
            }

            var analysis = new DistributionAnalysis();
            return analysis.Learn(distribution.Generate(10_000).ToArray());
        }

        private class GoodnessMetric
        {
            private GoodnessMetric(string method, double pValue, bool significant, int rank)
            {
                Method = method;
                PValue = pValue;
                Significant = significant;
                Rank = rank;
            }

            public string Method { get; }

            public double PValue { get; }

            public bool Significant { get; }

            public int Rank { get; }

            public static GoodnessMetric AndersonDarling(AndersonDarlingTest ad, int rank) =>
                new GoodnessMetric("AndersonDarling", ad.PValue, ad.Significant, rank);

            public static GoodnessMetric ChiSquare(ChiSquareTest cs, int rank) =>
                new GoodnessMetric("ChiSquare", cs.PValue, cs.Significant, rank);

            public static GoodnessMetric KolmogorovSmirnov(KolmogorovSmirnovTest ks, int rank) =>
                new GoodnessMetric("KolmogorovSmirnov", ks.PValue, ks.Significant, rank);
        }
    }
}