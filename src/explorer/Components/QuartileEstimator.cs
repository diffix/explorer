namespace Explorer.Components
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using Explorer.Common;
    using Explorer.Metrics;

    public class QuartileEstimator : PublisherComponent
    {
        private readonly ResultProvider<DistinctValuesComponent.Result> distinctValuesProvider;
        private readonly ResultProvider<Histogram> histogramResultProvider;

        public QuartileEstimator(
            ResultProvider<DistinctValuesComponent.Result> distinctValuesProvider,
            ResultProvider<Histogram> histogramResultProvider)
        {
            this.distinctValuesProvider = distinctValuesProvider;
            this.histogramResultProvider = histogramResultProvider;
        }

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
            var distinctValues = await distinctValuesProvider.ResultAsync;
            if (distinctValues == null)
            {
                yield break;
            }
            if (distinctValues.IsCategorical)
            {
                yield break;
            }

            var histogramResult = await histogramResultProvider.ResultAsync;
            if (histogramResult == null)
            {
                yield break;
            }
            var quartiles = await EstimateQuartiles(histogramResult);
            quartiles.Sort();
            yield return ExploreMetric.Create(MetricDefinitions.QuartileEstimates, quartiles);
        }
    }
}