namespace Explorer.Explorers.Components
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.Json;
    using System.Threading.Tasks;

    using Diffix;
    using Explorer.Common;
    using Explorer.Queries;

    internal class DistinctValuesComponent
        : ExplorerComponent<DistinctValuesComponent.Result>
    {
        private readonly DConnection conn;
        private readonly ExplorerContext ctx;

        public DistinctValuesComponent(DConnection conn, ExplorerContext ctx)
        {
            this.ctx = ctx;
            this.conn = conn;
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

        public class Result : Metrics.MetricsProvider
        {
            private const double SuppressedRatioThreshold = 0.1;

            public Result(
                IEnumerable<ValueWithCount<JsonElement>> distinctRows,
                ValueCounts valueCounts)
            {
                DistinctRows = distinctRows;
                ValueCounts = valueCounts;
            }

            public IEnumerable<ValueWithCount<JsonElement>> DistinctRows { get; }

            public ValueCounts ValueCounts { get; }

            public IEnumerable<ExploreMetric> Metrics()
            {
                if (ValueCounts.SuppressedRowRatio < SuppressedRatioThreshold)
                {
                    // Only few of the values are suppressed. This means the data is already well-segmented and can be
                    // considered categorical or quasi-categorical.
                    var distinctValues =
                        from row in DistinctRows
                        where row.HasValue
                        orderby row.Count descending
                        select new
                        {
                            row.Value,
                            row.Count,
                        };

                    yield return new UntypedMetric(name: "distinct.values", metric: distinctValues);
                    yield return new UntypedMetric(name: "distinct.null_count", metric: ValueCounts.NullCount);
                    yield return new UntypedMetric(name: "distinct.suppressed_count", metric: ValueCounts.SuppressedCount);
                    yield return new UntypedMetric(name: "distinct.value_count", metric: ValueCounts.TotalCount);
                }
            }
        }
    }
}