namespace Explorer.Explorers.Components
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Diffix;
    using Explorer.Common;
    using Explorer.Explorers.Metrics;

    internal class QuartileEstimator :
        ExplorerComponent<QuartileEstimator.Result>,
        DependsOn<NumericHistogramComponent.Result>
    {
        private ExplorerComponent<NumericHistogramComponent.Result>? histogramComponent;

        public QuartileEstimator(DConnection conn, ExplorerContext ctx)
        : base(conn, ctx)
        {
        }

        public void LinkToSourceComponent(ExplorerComponent<NumericHistogramComponent.Result> component)
        {
            histogramComponent = component;
        }

        protected override async Task<Result> Explore()
        {
            histogramComponent ??= new NumericHistogramComponent(Conn, Ctx);

            var selectedHistogram = await histogramComponent.ResultAsync;

            var quartileEstimates = new List<double>();
            var quartileCount = selectedHistogram.ValueCounts.NonSuppressedNonNullCount / 4;
            var quartile = 1;
            var processed = 0L;

            foreach (var bucket in selectedHistogram.Buckets.Where(b => b.HasValue))
            {
                if (processed + bucket.Count < quartileCount * quartile)
                {
                    // no quartiles in this bucket
                    processed += bucket.Count;
                }
                else
                {
                    // one or more quartiles in this bucket
                    var remaining = bucket.Count;
                    var lowerBound = bucket.LowerBound.Value;
                    var range = (double)selectedHistogram.BucketSize;

                    do
                    {
                        var toProcess = (quartileCount * quartile) - processed;

                        if (toProcess > remaining)
                        {
                            processed += remaining;
                            break;
                        }

                        var subRange = (double)toProcess / remaining * range;
                        var quartileEstimate = lowerBound + subRange;

                        quartileEstimates.Add(quartileEstimate);

                        lowerBound = quartileEstimate;
                        range -= subRange;
                        processed += toProcess;
                        remaining -= toProcess;
                        quartile++;
                    }
                    while (remaining > 0 && quartile <= 3);

                    if (quartile > 3)
                    {
                        break;
                    }
                }
            }

            return new Result(quartileEstimates);
        }

        public class Result : MetricsProvider
        {
            public Result(List<double> quartiles)
            {
                if (quartiles.Count != 3)
                {
                    throw new System.Exception($"Expected three quartile values, got {quartiles.Count}.");
                }

                AsList = quartiles;
                AsList.Sort();
            }

            public List<double> AsList { get; }

            public double Q1 { get => AsList[0]; }

            public double Q2 { get => AsList[1]; }

            public double Q3 { get => AsList[2]; }

            public IEnumerable<ExploreMetric> Metrics()
            {
                yield return new UntypedMetric(name: "quartile_estimates", metric: AsList);
            }
        }
    }
}