namespace Explorer.Explorers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using Diffix;
    using Explorer.Common;
    using Explorer.Queries;

    internal class RealColumnExplorer : ExplorerBase
    {
        // TODO: The following should be configuration items (?)
        private const long ValuesPerBucketTarget = 20;

        private const double SuppressedRatioThreshold = 0.1;

        public override async Task Explore(DConnection conn, ExplorerContext ctx)
        {
            var statsQ = await conn.Exec<NumericColumnStats.Result<double>>(
                new NumericColumnStats(ctx.Table, ctx.Column));

            var stats = statsQ.Rows.Single();

            PublishMetric(new UntypedMetric(name: "naive_min", metric: stats.Min));
            PublishMetric(new UntypedMetric(name: "naive_max", metric: stats.Max));

            // var distinctValueQ = await conn.Exec(
            //     new DistinctColumnValues(ctx.Table, ctx.Column));

            // var counts = ValueCounts.Compute(distinctValueQ.Rows);

            // if (counts.TotalCount == 0)
            // {
            //     throw new Exception(
            //         $"Total value count for {ctx.Table}, {ctx.Column} is zero.");
            // }

            // if (counts.SuppressedCountRatio < SuppressedRatioThreshold)
            // {
            //     // Only few of the values are suppressed. This means the data is already well-segmented and can be
            //     // considered categorical or quasi-categorical.
            //     var distinctValues =
            //         from row in distinctValueQ.Rows
            //         where row.HasValue
            //         orderby row.Count descending
            //         select new
            //         {
            //             row.Value,
            //             row.Count,
            //         };

            //     PublishMetric(new UntypedMetric(name: "distinct.values", metric: distinctValues));
            //     PublishMetric(new UntypedMetric(name: "distinct.null_count", metric: counts.NullCount));
            //     PublishMetric(new UntypedMetric(name: "distinct.suppressed_count", metric: counts.SuppressedCount));

            //     return;
            // }

            var bucketsToSample = BucketUtils.EstimateBucketResolutions(
                stats.Count, stats.Min, stats.Max, ValuesPerBucketTarget);

            var histogramQ = await conn.Exec(new SingleColumnHistogram(ctx.Table, ctx.Column, bucketsToSample));

            var histograms = histogramQ.Rows
                .GroupBy(
                    result => result.GroupingLabel,
                    (label, result) => new
                    {
                        BucketSize = label,
                        ValueCounts = ValueCounts.Compute(result),
                        Buckets = result.Where(b => b.HasValue).OrderBy(b => b.Value),
                    });

            var selectedHistogram = histograms
                .OrderBy(h => h.ValueCounts.SuppressedCount)
                .ThenBy(h => h.BucketSize)
                .First();

            PublishMetric(new UntypedMetric(
                name: "histogram.buckets",
                metric: selectedHistogram
                            .Buckets
                            .Where(b => b.HasValue)
                            .Select(b => new
                            {
                                selectedHistogram.BucketSize,
                                b.LowerBound,
                                b.Count,
                            })));
            PublishMetric(new UntypedMetric(name: "histogram.suppressed_count", metric: selectedHistogram.ValueCounts.SuppressedCount));
            PublishMetric(new UntypedMetric(name: "histogram.suppressed_ratio", metric: selectedHistogram.ValueCounts.SuppressedCountRatio));
            PublishMetric(new UntypedMetric(name: "histogram.value_counts", metric: selectedHistogram.ValueCounts));

            // Estimate Quartiles
            var quartileEstimates = new List<double>();
            var processed = 0L;
            var quartileCount = selectedHistogram.ValueCounts.NonSuppressedNonNullCount / 4;
            var quartile = 1;
            var valueBuckets = selectedHistogram.Buckets.Where(b => b.HasValue);
            foreach (var bucket in valueBuckets)
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
                    var lowerBound = bucket.Value;
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

            PublishMetric(new UntypedMetric(name: "quartile_estimates", metric: quartileEstimates));

            // Estimate Average
            var averageEstimate = valueBuckets
                .Sum(bucket => bucket.Count * ((decimal)bucket.Value + (selectedHistogram.BucketSize / 2)))
                / selectedHistogram.ValueCounts.NonSuppressedNonNullCount;

            PublishMetric(new UntypedMetric(name: "avg_estimate", metric: decimal.Round(averageEstimate, 2)));
        }
    }
}
