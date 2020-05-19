namespace Explorer.Components
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.Json;
    using System.Threading.Tasks;

    using Diffix;
    using Explorer.Common;
    using Explorer.Metrics;
    using Explorer.Queries;

    public class DistinctValuesComponent
        : ExplorerComponent<DistinctValuesComponent.Result>, PublisherComponent
    {
        private const double SuppressedRatioThreshold = 0.1;
        private readonly DConnection conn;
        private readonly ExplorerContext ctx;

        public DistinctValuesComponent(DConnection conn, ExplorerContext ctx)
        {
            this.ctx = ctx;
            this.conn = conn;
        }

        public async IAsyncEnumerable<ExploreMetric> YieldMetrics()
        {
            var result = await ResultAsync;

            var valueCounts = result.ValueCounts;

            if (valueCounts.SuppressedRowRatio < SuppressedRatioThreshold)
            {
                // Only few of the values are suppressed. This means the data is already well-segmented and can be
                // considered categorical or quasi-categorical.
                var distinctValues =
                    from row in result.DistinctRows
                    where row.HasValue
                    orderby row.Count descending
                    select new
                    {
                        row.Value,
                        row.Count,
                    };

                yield return new UntypedMetric(name: "distinct.is_categorical", metric: true);
                yield return new UntypedMetric(name: "distinct.values", metric: distinctValues);
                yield return new UntypedMetric(name: "distinct.null_count", metric: valueCounts.NullCount);
                yield return new UntypedMetric(name: "distinct.suppressed_count", metric: valueCounts.SuppressedCount);
                yield return new UntypedMetric(name: "distinct.value_count", metric: valueCounts.TotalCount);
            }
            else
            {
                yield return new UntypedMetric(name: "distinct.is_categorical", metric: false);
            }
        }

        protected override async Task<Result> Explore()
        {
            var distinctValueQ = await conn.Exec(
                new DistinctColumnValues(ctx.Table, ctx.Column));

            var counts = ValueCounts.Compute(distinctValueQ.Rows);

            if (counts.TotalCount == 0)
            {
                throw new Exception(
                    $"Total value count for {ctx.Table}, {ctx.Column} is zero.");
            }

            return new Result(distinctValueQ.Rows, counts);
        }

        public class Result
        {

            public Result(
                IEnumerable<ValueWithCount<JsonElement>> distinctRows,
                ValueCounts valueCounts)
            {
                DistinctRows = distinctRows;
                ValueCounts = valueCounts;
            }

            public IEnumerable<ValueWithCount<JsonElement>> DistinctRows { get; }

            public ValueCounts ValueCounts { get; }
        }
    }
}