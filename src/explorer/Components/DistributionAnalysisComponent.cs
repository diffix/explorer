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

            yield return new UntypedMetric(
                name: "distribution_estimates",
                metric: fits.Values
                    .OrderBy(fit => fit.Index)
                    .Take(3)
                    .Select(fit =>
                    new
                    {
                        fit.Name,
                        Distribution = fit.Distribution.ToString(),
                        Goodness = fit.KolmogorovSmirnov,
                    }));
        }

        protected override async Task<GoodnessOfFitCollection> Explore()
        {
            var distribution = await distributionProvider.ResultAsync;

            return await Task.Run(() =>
            {
                var analysis = new DistributionAnalysis();
                return analysis.Learn(distribution.Generate(1_000));
            });
        }
    }
}