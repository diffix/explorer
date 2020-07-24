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

            foreach (var m in TimeUtilities.YieldMetrics<Result, DateTime>("dates_linear", result))
            {
                yield return m;
            }
        }

        protected override async Task<Result> Explore()
        {
            var queryResult = await conn.Exec(
                new BucketedDatetimes(ctx.Table, ctx.Column, ctx.ColumnInfo.Type));

            var groupings = await Task.Run(() => ProcessLinearBuckets(queryResult.Rows));

            return new Result(
                groupings.Select(g => g.Item1),
                groupings.Select(g => g.Item2));
        }

        private static IEnumerable<(ValueCounts, IGrouping<string, GroupingSetsResult<DateTime>>)> ProcessLinearBuckets(
            IEnumerable<GroupingSetsResult<DateTime>> queryResult)
        {
            foreach (var group in TimeUtilities.GroupByLabel(queryResult))
            {
                var counts = ValueCounts.Compute(group);
                if (counts.SuppressedRowRatio > SuppressedRatioThreshold)
                {
                    break;
                }

                yield return (counts, group);
            }
        }

        public class Result : TimeUtilities.GenericResult<DateTime>
        {
            public Result(
                IEnumerable<ValueCounts> valueCounts,
                IEnumerable<IGrouping<string, GroupingSetsResult<DateTime>>> rows)
            : base(valueCounts, rows)
            {
            }
        }
    }
}