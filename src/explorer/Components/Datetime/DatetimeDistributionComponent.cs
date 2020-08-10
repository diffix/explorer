namespace Explorer.Components
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using Accord.Statistics.Distributions.Univariate;
    using Explorer.Metrics;

    public class DatetimeDistributionComponent : ExplorerComponent<DatetimeDistribution>, PublisherComponent
    {
        private readonly ResultProvider<LinearTimeBuckets.Result> timeBucketsResultProvider;

        public DatetimeDistributionComponent(ResultProvider<LinearTimeBuckets.Result> timeBucketsResultProvider)
        {
            this.timeBucketsResultProvider = timeBucketsResultProvider;
        }

        public static DatetimeDistribution? GenerateDistribution(LinearTimeBuckets.Result timeBuckets)
        {
            if (!timeBuckets.Rows.Any())
            {
                return null;
            }

            var (bucketGroup, valueCounts) = timeBuckets.Rows.Zip(timeBuckets.ValueCounts).Last();
            var timeUnit = bucketGroup.Key;

            var offsetCounts = bucketGroup
                .Where(bucket => bucket.HasValue)
                .Select(bucket =>
                {
                    var offsetSpan = bucket.Value - DateTime.UnixEpoch;

                    var offset = timeUnit switch
                    {
                        "hour" => offsetSpan.TotalHours,
                        "minute" => offsetSpan.TotalMinutes,
                        "second" => offsetSpan.TotalSeconds,

                        // Note: System.TimeSpan does not provide any larger denomination than days
                        _ => offsetSpan.TotalDays
                    };

                    // Let's assume it's ok to convert Count from `long` to `int`
                    // (ie. each group contains fewer than 2147483647 values)
                    return (Offset: offset, Count: Convert.ToInt32(bucket.Count));
                });

            var distribution = new EmpiricalDistribution(
                                        offsetCounts.Select(_ => _.Offset).ToArray(),
                                        offsetCounts.Select(_ => _.Count).ToArray());

            return new DatetimeDistribution(timeUnit, distribution);
        }

        public async IAsyncEnumerable<ExploreMetric> YieldMetrics()
        {
            var result = await ResultAsync;
            if (result == null)
            {
                yield break;
            }

            yield return new UntypedMetric(name: "descriptive_stats", metric: result);
        }

        protected override async Task<DatetimeDistribution?> Explore()
        {
            var timeBuckets = await timeBucketsResultProvider.ResultAsync;
            if (timeBuckets == null)
            {
                return null;
            }

            return GenerateDistribution(timeBuckets);
        }
    }
}