namespace Explorer.Components
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using Diffix;
    using Explorer.Common;
    using Explorer.Metrics;
    using Explorer.Queries;

    public class LinearTimeBuckets : ExplorerComponent<LinearTimeBuckets.Result>, PublisherComponent
    {
        private const double SuppressedRatioThreshold = 0.1;

        private readonly DConnection conn;
        private readonly ExplorerContext ctx;

        public LinearTimeBuckets(DConnection conn, ExplorerContext ctx)
        {
            this.conn = conn;
            this.ctx = ctx;
        }

        public async IAsyncEnumerable<ExploreMetric> YieldMetrics()
        {
            var result = await ResultAsync;

            foreach (var (valueCount, row) in result.ValueCounts.Zip(result.Rows))
            {
                yield return new UntypedMetric(
                    name: $"dates_linear.{row.Key}",
                    metric: MetricBlob<DateTime>(valueCount.TotalCount, valueCount.SuppressedCount, row.Select(_ => _)));
            }
        }

        protected override async Task<Result> Explore()
        {
            var queryResult = await conn.Exec(
                new BucketedDatetimes(ctx.Table, ctx.Column, ctx.ColumnType));

            var groupings = await Task.Run(() => ProcessLinearBuckets(queryResult.Rows));

            return new Result(
                groupings.Select(g => g.Item1),
                groupings.Select(g => g.Item2));
        }

        private IEnumerable<(ValueCounts, IGrouping<string, GroupingSetsResult<DateTime>>)> ProcessLinearBuckets(
            IEnumerable<GroupingSetsResult<DateTime>> queryResult)
        {
            foreach (var group in GroupByLabel(queryResult))
            {
                var counts = ValueCounts.Compute(group);
                if (counts.SuppressedCountRatio > SuppressedRatioThreshold)
                {
                    break;
                }

                yield return (counts, group);
            }
        }

        private IEnumerable<IGrouping<string, GroupingSetsResult<T>>> GroupByLabel<T>(
            IEnumerable<GroupingSetsResult<T>> queryResult)
        {
            return queryResult.GroupBy(row => row.GroupingLabel);
        }

        private static object MetricBlob<T>(
            long total,
            long suppressed,
            IEnumerable<GroupingSetsResult<T>> valueCounts)
        {
            return new
            {
                Total = total,
                Suppressed = suppressed,
                Counts =
                    from row in valueCounts
                    where row.HasValue
                    orderby row.Value ascending
                    select new
                    {
                        row.Value,
                        row.Count,
                        row.CountNoise,
                    },
            };
        }

        public class Result : GenericResult<DateTime>
        {
            public Result(
                IEnumerable<ValueCounts> valueCounts,
                IEnumerable<IGrouping<string, GroupingSetsResult<DateTime>>> rows)
            : base(valueCounts, rows)
            {
            }
        }

        public class GenericResult<T>
        {
            public GenericResult(
                IEnumerable<ValueCounts> valueCounts,
                IEnumerable<IGrouping<string, GroupingSetsResult<T>>> rows)
            {
                ValueCounts = valueCounts;
                Rows = rows;
            }

            public IEnumerable<ValueCounts> ValueCounts { get; }

            public IEnumerable<IGrouping<string, GroupingSetsResult<T>>> Rows { get; }
        }
    }
}