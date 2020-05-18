namespace Explorer.Components
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using Explorer.Common;

    public class QuartileEstimator :
        ExplorerComponent<QuartileEstimator.Result>
    {
        private readonly ResultProvider<NumericHistogramComponent.Result> histogramResult;

        public QuartileEstimator(
            ResultProvider<NumericHistogramComponent.Result> histogramResult)
        {
            this.histogramResult = histogramResult;
        }

        public static Task<List<double>> EstimateQuartiles(NumericHistogramComponent.Result result) =>
            EstimateQuartiles(result.Histogram);

        public static Task<List<double>> EstimateQuartiles(Histogram histogram) => Task.Run(() =>
        {
            var quartileEstimates = new List<double>();
            var quartileCount = histogram.Buckets.Sum(h => h.Value.Count) / 4;
            var quartile = 1;
            var processed = 0L;

            foreach (var (bucketSize, bucket) in histogram.Buckets)
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
                    var lowerBound = (double)bucket.LowerBound;
                    var range = (double)bucketSize;

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

            return quartileEstimates;
        });

        protected override async Task<Result> Explore() =>
            new Result(await EstimateQuartiles(await histogramResult.ResultAsync));

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