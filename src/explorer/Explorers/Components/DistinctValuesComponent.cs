namespace Explorer.Components
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json;
    using System.Threading.Tasks;

    using Diffix;
    using Explorer.Common;
    using Explorer.Queries;

    public class DistinctValuesComponent
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