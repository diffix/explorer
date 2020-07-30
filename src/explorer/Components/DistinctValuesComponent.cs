namespace Explorer.Components
{
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
        private const int DefaultNumValuesToPublish = 10;
        private readonly DConnection conn;
        private readonly ExplorerContext ctx;

        public DistinctValuesComponent(DConnection conn, ExplorerContext ctx)
        {
            this.ctx = ctx;
            this.conn = conn;
        }

        public int NumValuesToPublish { get; set; } = DefaultNumValuesToPublish;

        public IEnumerable<ExploreMetric> YieldMetrics(Result result)
        {
            if (!result.IsCategorical)
            {
                yield return new UntypedMetric(name: "distinct.is_categorical", metric: false);
            }
            else
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

                var toPublish = distinctValues.Take(NumValuesToPublish);
                var remaining = distinctValues.Skip(NumValuesToPublish);

                if (remaining.Any())
                {
                    toPublish = toPublish.Append(new
                    {
                        Value = JsonDocument.Parse("\"--OTHER--\"").RootElement,
                        Count = remaining.Sum(distinct => distinct.Count),
                    });
                }

                var valueCounts = result.ValueCounts;
                yield return new UntypedMetric(name: "distinct.is_categorical", metric: true);
                yield return new UntypedMetric(name: "distinct.values", metric: toPublish.ToList());
                yield return new UntypedMetric(name: "distinct.null_count", metric: valueCounts.NullCount);
                yield return new UntypedMetric(name: "distinct.suppressed_count", metric: valueCounts.SuppressedCount);
                yield return new UntypedMetric(name: "distinct.value_count", metric: valueCounts.TotalCount);
            }
        }

        public async IAsyncEnumerable<ExploreMetric> YieldMetrics()
        {
            foreach (var m in YieldMetrics(await ResultAsync))
            {
                yield return m;
            }
        }

        protected override async Task<Result> Explore()
        {
            if (ctx.ColumnInfo.UserId)
            {
                return new Result(Enumerable.Empty<ValueWithCount<JsonElement>>());
            }
            var distinctValueResult = await conn.Exec(new DistinctColumnValues(ctx.Table, ctx.Column));
            return new Result(distinctValueResult.Rows.OrderByDescending(r => r.Count));
        }

        public class Result
        {
            public Result(IEnumerable<ValueWithCount<JsonElement>> distinctRows)
            {
                DistinctRows = distinctRows;
                ValueCounts = ValueCounts.Compute(distinctRows);
                IsCategorical = ValueCounts.SuppressedRowRatio < SuppressedRatioThreshold;
            }

            public IEnumerable<ValueWithCount<JsonElement>> DistinctRows { get; }

            public ValueCounts ValueCounts { get; }

            public bool IsCategorical { get; }
        }
    }
}