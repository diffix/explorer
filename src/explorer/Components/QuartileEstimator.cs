namespace Explorer.Components
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using Explorer.Common;
    using Explorer.Common.Utils;
    using Explorer.Components.ResultTypes;

    public class QuartileEstimator :
        ExplorerComponent<QuartileEstimator.Result>, PublisherComponent
    {
        private readonly ResultProvider<DistinctValuesComponent.Result> distinctValuesProvider;
        private readonly ResultProvider<HistogramWithCounts> histogramResultProvider;

        public QuartileEstimator(
            ResultProvider<DistinctValuesComponent.Result> distinctValuesProvider,
            ResultProvider<HistogramWithCounts> histogramResultProvider)
        {
            this.distinctValuesProvider = distinctValuesProvider;
            this.histogramResultProvider = histogramResultProvider;
        }

        public static Task<List<double>> EstimateQuartiles(HistogramWithCounts hwc) =>
            EstimateQuartiles(hwc.Histogram);

        public static Task<List<double>> EstimateQuartiles(Histogram histogram) => Task.Run(() =>
        {
            var quartileEstimates = new List<double>();
            var quartileCount = histogram.Buckets.Sum(h => h.Count) / 4;
            var quartile = 1;
            var processed = 0L;

            foreach (var bucket in histogram.Buckets)
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
                    var start = (double)bucket.LowerBound;
                    var range = (double)bucket.BucketSize.SnappedSize;

                    do
                    {
                        var toProcess = (quartileCount * quartile) - processed;

                        if (toProcess >= remaining)
                        {
                            processed += remaining;
                            break;
                        }

                        var subRange = (double)toProcess / remaining * range;
                        var quartileEstimate = start + subRange;

                        quartileEstimates.Add(quartileEstimate);

                        start = quartileEstimate;
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

            return quartileEstimates;
        });

        public async IAsyncEnumerable<ExploreMetric> YieldMetrics()
        {
            var result = await ResultAsync;
            if (result == null)
            {
                yield break;
            }

            yield return new UntypedMetric(name: "quartile_estimates", metric: result.AsList);
        }

        protected override async Task<Result?> Explore()
        {
            var distinctValues = await distinctValuesProvider.ResultAsync;
            if (distinctValues == null)
            {
                return null;
            }
            if (distinctValues.IsCategorical)
            {
                return null;
            }

            var histogramResult = await histogramResultProvider.ResultAsync;
            if (histogramResult == null)
            {
                return null;
            }
            return new Result(await EstimateQuartiles(histogramResult));
        }

        public class Result
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
        }
    }
}